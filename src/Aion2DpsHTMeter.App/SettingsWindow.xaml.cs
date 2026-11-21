using System.Windows;
using System.Windows.Input;
using Aion2DpsHTMeter.App.ViewModels;

namespace Aion2DpsHTMeter.App;

/// <summary>Measurement targets and background opacity; changes apply at once and are saved on exit.</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(OverlayViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
