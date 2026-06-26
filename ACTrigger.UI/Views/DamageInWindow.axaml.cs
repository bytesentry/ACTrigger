using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System;
using ACTrigger.UI.Interop;

namespace ACTrigger.UI.Views;

public partial class DamageInWindow : Window
{
    private bool _layoutMode;
    
    public DamageInWindow()
    {
        InitializeComponent();

        PointerPressed += OnPointerPressed;
    }

    public void SetLayoutMode(bool enabled)
    {
        _layoutMode = enabled;

        var border =
            this.FindControl<Border>(
                "LayoutBorder");
        var label =
            this.FindControl<TextBlock>(
                "OverlayLabel");

        if (border == null)
            return;

        if (enabled)
        {
            border.Background =
                new SolidColorBrush(
                    Color.Parse("#66000000"));

            border.BorderBrush =
                Brushes.White;

            border.BorderThickness =
                new Thickness(1);

            if (label != null)
                label.IsVisible = true;
        }
        else
        {
            border.Background = null;
            border.IsHitTestVisible = false;

            border.BorderThickness =
                new Thickness(0);

            if (label != null)
                label.IsVisible = false;
        }
        if (OperatingSystem.IsWindows())
        {
            var handle =
                TryGetPlatformHandle();

            if (handle != null &&
                handle.HandleDescriptor ==
                "HWND")
            {
                WindowsOverlayHelper
                    .SetClickThrough(
                        handle.Handle,
                        !enabled);
            }
        }
        if (OperatingSystem.IsLinux())
        {
            var handle =
                TryGetPlatformHandle();

            if (handle != null &&
                handle.HandleDescriptor ==
                "XID")
            {
                LinuxOverlayHelper
                    .SetClickThrough(
                        handle.Handle,
                        !enabled);
            }
        }
    }
    private void OnPointerPressed(
        object? sender,
        PointerPressedEventArgs e)
    {
        if (!_layoutMode)
            return;

        BeginMoveDrag(e);
    }
}