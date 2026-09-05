using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TheOmegaStrain.Wpf.MainWindowClasses.Overlays;

public sealed class OverlayHostWindow : Window
{
    private readonly Window _ownerWindow;

    public Grid OverlayRoot { get; } = new()
    {
        Background = Brushes.Transparent,
        UseLayoutRounding = true,
        SnapsToDevicePixels = true
    };

    public OverlayHostWindow(Window owner)
    {
        _ownerWindow = owner ?? throw new ArgumentNullException(nameof(owner));
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        IsHitTestVisible = false;
        ShowActivated = false;
        Focusable = false;

        // Separate top-level window, so it does not inherit the main window's
        // Cursor="None". It covers the full play area, so without this the
        // arrow reappears over the game.
        Cursor = Cursors.None;
        Content = OverlayRoot;
    }

    public void ShowOverlay()
    {
        Owner ??= _ownerWindow;
        SynchronizeBounds();
        if (!IsVisible)
            Show();
        _ownerWindow.Activate();
    }

    public void SynchronizeBounds()
    {
        Left = _ownerWindow.Left;
        Top = _ownerWindow.Top;
        Width = Math.Max(1, _ownerWindow.ActualWidth);
        Height = Math.Max(1, _ownerWindow.ActualHeight);
    }
}
