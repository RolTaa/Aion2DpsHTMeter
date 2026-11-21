using System.IO;
using System.Text.Json;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Events;
using AionMeter.Core.Game;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>Owns the capture → decoder → combat tracker pipeline (all from AionMeter.Core).</summary>
public sealed class MeterService : IDisposable
{
    private readonly AppSettings _settings;
    private IEventSource? _capture;
    private DemoFightSource? _demo;

    private readonly FightFilter _fights;
    private readonly HashSet<int> _artifactNpcs;

    public MeterService(AppSettings settings)
    {
        _settings = settings;
        Data = GameData.Load(DataDirectory);
        Tracker = new CombatTracker(Data, new MeterOptions());
        _artifactNpcs = LoadArtifactNpcs(Path.Combine(DataDirectory, "abyss_artifact_npcs.json"));
        _fights = new FightFilter(_artifactNpcs);
    }

    public static string DataDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "data");

    public GameData Data { get; }
    public CombatTracker Tracker { get; }
    public bool DemoRunning => _demo?.IsRunning == true;

    public CaptureStatus CaptureStatus =>
        DemoRunning ? new CaptureStatus(CaptureState.Capturing, "Demo")
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

    /// <summary>Closes the running fight; the view stays empty until the next fight of a chosen kind.</summary>
    public void Reset()
    {
        Tracker.Reset();
        _fights.Forget();
    }

    /// <summary>Idle checks (fight end by timeout) run on the wall clock; call this from the UI timer.</summary>
    public void Tick() => Tracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    /// <summary>
    /// The running fight when it is against a chosen kind of target (settings: artifact, boss, trash), or the last such
    /// fight until the next one starts.
    /// </summary>
    public EncounterSnapshot? CurrentFight()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return _fights.Pick(Tracker.Snapshot(null, now), _settings.Targets, id => Tracker.Snapshot(id, now));
    }

    /// <summary>
    /// With the artifact siege chosen as a target: the Dimensional Core on this map to watch, hit or not. A living one
    /// before a dead one, and among the living the one with the least HP left (the one being fought over).
    /// </summary>
    public BossSnapshot? WatchedCore()
    {
        if ((_settings.Targets & TargetKinds.AbyssArtifact) == 0) return null;
        return Tracker.NpcsOnMap(_artifactNpcs)
            .Where(core => core.Hp >= 0)
            .OrderBy(core => core.Hp == 0)
            .ThenBy(core => core.HpFraction)
            .FirstOrDefault();
    }

    /// <summary>NPC codes of the artifact-siege targets: the keys of the file's "npcs" object.</summary>
    private static HashSet<int> LoadArtifactNpcs(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.GetProperty("npcs").EnumerateObject()
                .Select(p => int.TryParse(p.Name, out var code) ? code : 0)
                .Where(code => code > 0)
                .ToHashSet();
        }
        catch (Exception ex) when (ex is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return [];
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
