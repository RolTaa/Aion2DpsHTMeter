using System.IO;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>Owns the capture → decoder → combat tracker pipeline (all from AionMeter.Core).</summary>
public sealed class MeterService : IDisposable
{
    /// <summary>The server resends the roster every few seconds; one older than this means we left the party.</summary>
    private static readonly TimeSpan RosterLifetime = TimeSpan.FromSeconds(60);

    private readonly AppSettings _settings;
    private IEventSource? _capture;
    private DemoFightSource? _demo;

    private readonly object _partyGate = new();
    private IReadOnlyList<PartyMemberInfo> _party = [];
    private DateTime _partySeenAt;

    /// <summary>The last fight that had a boss: shown until the next boss fight starts.</summary>
    private Guid? _lastBossFight;

    public MeterService(AppSettings settings)
    {
        _settings = settings;
        Data = GameData.Load(DataDirectory);
        Tracker = new CombatTracker(Data, new MeterOptions());
        Tracker.RosterReceived += OnRoster;
    }

    public static string DataDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "data");

    public GameData Data { get; }
    public CombatTracker Tracker { get; }

    public CaptureStatus CaptureStatus =>
        _demo?.IsRunning == true ? new CaptureStatus(CaptureState.Capturing, "Demo")
        : _capture?.Status ?? CaptureStatus.Stopped;

    public void StartCapture()
    {
        if (_capture is not null) return;
        if (!LiveCapture.IsNpcapInstalled())
        {
            _capture = new FailedSource("Npcap が見つかりません(WinPcap API 互換モードでインストールしてください)");
            return;
        }
        try
        {
            _capture = new LiveCapture(Data, new LiveCaptureOptions { DeviceName = _settings.CaptureDevice });
            _capture.EventDecoded += Tracker.Process;
            _capture.Start();
        }
        catch (Exception ex)
        {
            _capture?.Dispose();
            _capture = new FailedSource(ex.Message);
        }
    }

    public void StartDemo(int seconds = 45)
    {
        _demo?.Dispose();
        _demo = new DemoFightSource(Tracker.Process);
        _demo.Start(TimeSpan.FromSeconds(seconds));
    }

    /// <summary>Closes the running fight; the view stays empty until the next boss fight.</summary>
    public void Reset()
    {
        Tracker.Reset();
        _lastBossFight = null;
    }

    /// <summary>Idle checks (fight end by timeout) run on the wall clock; call this from the UI timer.</summary>
    public void Tick() => Tracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    /// <summary>
    /// The running boss fight, or the last one until the next boss is pulled. Fights without a boss (trash, open
    /// world mobs) are never shown.
    /// </summary>
    public EncounterSnapshot? BossFight()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var live = Tracker.Snapshot(null, now);
        if (live?.Boss is not null)
        {
            _lastBossFight = live.Id;
            return live;
        }
        return _lastBossFight is { } id ? Tracker.Snapshot(id, now) : null;
    }

    /// <summary>The local player, and the players on the latest party roster (by name, and server when both are known).</summary>
    public bool IsPartyMember(CombatantSnapshot c)
    {
        if (c.IsSelf || _demo?.IsRunning == true) return true; // the demo fight sends no roster
        lock (_partyGate)
        {
            if (DateTime.UtcNow - _partySeenAt > RosterLifetime) return false;
            return _party.Any(m => m.Name == c.Name && (m.ServerId == 0 || c.ServerId == 0 || m.ServerId == c.ServerId));
        }
    }

    private void OnRoster(PartyRosterEvent roster)
    {
        lock (_partyGate)
        {
            _party = roster.Members;
            _partySeenAt = DateTime.UtcNow;
        }
    }

    public void Dispose()
    {
        _capture?.Dispose();
        _demo?.Dispose();
    }

    /// <summary>Placeholder source that only reports why capture could not start.</summary>
    private sealed class FailedSource(string message) : IEventSource
    {
        public event Action<GameEvent>? EventDecoded { add { } remove { } }
        public event Action<CaptureStatus>? StatusChanged { add { } remove { } }
        public CaptureStatus Status { get; } = new(CaptureState.Error, message);
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }
}
