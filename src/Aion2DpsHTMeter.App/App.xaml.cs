using System.Windows;
using Aion2DpsHTMeter.App.Services;
using Aion2DpsHTMeter.App.ViewModels;

namespace Aion2DpsHTMeter.App;

public partial class App : Application
{
    private MeterService? _meter;
    private AppSettings? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = AppSettings.Load();
        _meter = new MeterService(_settings);

        // --demo: a generated fight instead of the live capture, to check the overlay without the game.
        if (e.Args.Contains("--demo")) _meter.StartDemo();
        else _meter.StartCapture();

        var vm = new OverlayViewModel(_meter, _settings);
        var window = new OverlayWindow(vm, _settings);
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _meter?.Dispose();
        _settings?.Save();
        base.OnExit(e);
    }
}
