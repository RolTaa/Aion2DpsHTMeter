using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Aion2DpsHTMeter.App.Services;

/// <summary>
/// Makes a window ignore the mouse (clicks go to the game below) and registers the global hotkey that turns it back
/// off — once click-through is on, the overlay's own buttons can no longer be clicked.
/// </summary>
public sealed class ClickThrough : IDisposable
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const int WM_HOTKEY = 0x0312;
    private const uint MOD_CONTROL = 0x2, MOD_SHIFT = 0x4, MOD_NOREPEAT = 0x4000;
    private const uint VK_L = 0x4C;
    private const int HotkeyId = 0xA102;

    private readonly Window _window;
    private HwndSource? _source;
    private bool _hotkey;

    public ClickThrough(Window window)
    {
        _window = window;
        _window.SourceInitialized += OnSourceInitialized;
    }

    public bool Enabled { get; private set; }

    public event Action<bool>? Changed;

    public void Toggle() => Set(!Enabled);

    public void Set(bool enabled)
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == IntPtr.Zero) return;
        var style = GetWindowLong(hwnd, GWL_EXSTYLE);
        style = enabled ? style | WS_EX_TRANSPARENT | WS_EX_LAYERED : style & ~WS_EX_TRANSPARENT;
        SetWindowLong(hwnd, GWL_EXSTYLE, style);
        Enabled = enabled;
        Changed?.Invoke(enabled);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var hwnd = new WindowInteropHelper(_window).Handle;
        // Never take the focus from the game.
        SetWindowLong(hwnd, GWL_EXSTYLE, GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_NOACTIVATE);
        _source = HwndSource.FromHwnd(hwnd);
        _source.AddHook(WndProc);
        _hotkey = RegisterHotKey(hwnd, HotkeyId, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, VK_L); // Ctrl+Shift+L
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            Toggle();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hotkey) UnregisterHotKey(new WindowInteropHelper(_window).Handle, HotkeyId);
        _source?.RemoveHook(WndProc);
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
