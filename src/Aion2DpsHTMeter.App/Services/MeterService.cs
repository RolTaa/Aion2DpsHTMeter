using System.IO;
using AionMeter.Core.Capture;
using AionMeter.Core.Combat;
using AionMeter.Core.Demo;
using AionMeter.Core.Game;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>Owns the capture → decoder → combat tracker pipeline (all from AionMeter.Core).</summary>
public sealed class MeterService : IDisposable
{
    private readonly AppSettings _settings;
    private IEventSource? _capture;
    private DemoFightSource? _demo;

    public MeterService(AppSettings settings)
    {
        _settings = settings;
        Data = GameData.Load(DataDirectory);
        Tracker = new CombatTracker(Data, new MeterOptions());
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

    /// <summary>Closes the running fight; the view stays empty until the next hit.</summary>
    public void Reset() => Tracker.Reset();

    /// <summary>Idle checks (fight end by timeout) run on the wall clock; call this from the UI timer.</summary>
    public void Tick() => Tracker.Tick(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    public EncounterSnapshot? Snapshot() => Tracker.Snapshot(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

    public void Dispose()
    {
        _capture?.Dispose();
        _demo?.Dispose();
    }

    /// <summary>Placeholder source that only reports why capture could not start.</summary>
    private sealed class FailedSource(string message) : IEventSource
    {
        public event Action<AionMeter.Core.Events.GameEvent>? EventDecoded { add { } remove { } }
        public event Action<CaptureStatus>? StatusChanged { add { } remove { } }
        public CaptureStatus Status { get; } = new(CaptureState.Error, message);
        public void Start() { }
        public void Stop() { }
        public void Dispose() { }
    }
}
