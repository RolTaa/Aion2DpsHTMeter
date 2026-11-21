using System.Windows;
using System.Windows.Input;
using Aion2DpsHTMeter.App.Services;
using Aion2DpsHTMeter.App.ViewModels;

namespace Aion2DpsHTMeter.App;

public partial class OverlayWindow : Window
{
    private readonly OverlayViewModel _vm;
    private readonly AppSettings _settings;
    private readonly ClickThrough _clickThrough;

    public OverlayWindow(OverlayViewModel vm, AppSettings settings)
    {
        InitializeComponent();
        _vm = vm;
        _settings = settings;
        DataContext = vm;

        if (settings.Left is { } left && settings.Top is { } top)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = left;
            Top = top;
        }
        LocationChanged += (_, _) =>
        {
            _settings.Left = Left;
            _settings.Top = Top;
        };

        _clickThrough = new ClickThrough(this);
        _clickThrough.Changed += on => ClickThroughButton.Opacity = on ? 0.5 : 1;
        Closed += (_, _) => _clickThrough.Dispose();
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void OnClickThrough(object sender, RoutedEventArgs e) => _clickThrough.Toggle();

    private void OnReset(object sender, RoutedEventArgs e) => _vm.Reset();

    private void OnNextScope(object sender, RoutedEventArgs e) => _vm.NextScope();

    private void OnSettings(object sender, RoutedEventArgs e) => new SettingsWindow(_vm) { Owner = this }.ShowDialog();

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
