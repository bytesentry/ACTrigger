using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System;
using ACTrigger.UI.Interop;
using ACTrigger.UI.Models;
using System.IO;
using ACTrigger.UI.ViewModels;
using System.ComponentModel;

namespace ACTrigger.UI.Views;

public partial class DebuffWindow : Window
{
    private bool _layoutMode;

    public DebuffWindow()
    {
        InitializeComponent();

        PointerPressed += OnPointerPressed;

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(
        object? sender,
        EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        vm.PropertyChanged += Vm_PropertyChanged;

        UpdateScale(vm);
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
            border.IsHitTestVisible = true;

            border.BorderThickness =
                new Thickness(0);

            if (label != null)
                label.IsVisible = false;
        }

        if (OperatingSystem.IsWindows())
        {
            var handle = TryGetPlatformHandle();
            if (handle != null && handle.HandleDescriptor == "HWND")
            {
                if (enabled)
                {
                    WindowsOverlayHelper.SetClickThrough(handle.Handle, false);
                }
                else if (DataContext is MainWindowViewModel vm && vm.AllowTargeting && vm.SessionActive)
                {
                    WindowsOverlayHelper.SetDebuffWindowStyle(handle.Handle);
                }
                else
                {
                    WindowsOverlayHelper.SetClickThrough(handle.Handle, true);
                }
            }
        }

        if (OperatingSystem.IsLinux())
        {
            var handle =
                TryGetPlatformHandle();

            if (handle != null &&
                handle.HandleDescriptor == "XID")
            {
                if (enabled)
                {
                    LinuxOverlayHelper
                        .SetClickThrough(
                            handle.Handle,
                            false);
                }
                else
                {
                    if (DataContext is MainWindowViewModel vm &&
                        vm.AllowTargeting && vm.SessionActive)
                    {
                        LinuxOverlayHelper
                            .SetDebuffInputRegion(
                                handle.Handle,
                                true,
                                vm.DebuffScale);
                    }
                    else
                    {
                        LinuxOverlayHelper
                            .SetClickThrough(
                                handle.Handle,
                                true);
                    }
                }
            }
        }
        
    }
    
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var handle =
            this.TryGetPlatformHandle()?.Handle;

        if (!handle.HasValue ||
            handle.Value == IntPtr.Zero)
        {
            return;
        }

        if (OperatingSystem.IsLinux())
        {
            LinuxOverlayHelper.SetNoFocus(
                handle.Value);
        }
        else if (OperatingSystem.IsWindows())
        {
            //WindowsOverlayHelper.SetDebuffWindowStyle(handle.Value);
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

    private void TargetName_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        
        if (sender is not Button button)
            return;

        if (button.DataContext is not DebuffGroup group)
            return;

        //Console.WriteLine(
        //    $"ACTIVE WINDOW: {TopLevel.GetTopLevel(this)?.IsActive}");

        if (DataContext is MainWindowViewModel vm)
        {
            var targetFile =
                Path.Combine(
                    Path.GetDirectoryName(
                        vm.LogPath)!,
                    "actrigger.target");

            File.WriteAllText(
                targetFile,
                group.TargetId.ToString());
        }
    }

    private void UpdateScale(
        MainWindowViewModel vm)
    {
        Width =
            240 * vm.DebuffScale;

        Height =
            350 * vm.DebuffScale;
    }

    private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.DebuffScale) &&
            DataContext is MainWindowViewModel vm)
        {
            UpdateScale(vm);
            ReapplyInputRegion(vm);
        }
    }

    private void ReapplyInputRegion(MainWindowViewModel vm)
    {
        if (!OperatingSystem.IsLinux())
            return;

        var handle = TryGetPlatformHandle();
        if (handle == null || handle.HandleDescriptor != "XID")
            return;

        if (vm.AllowTargeting)
        {
            LinuxOverlayHelper.SetDebuffInputRegion(
                handle.Handle,
                true,
                vm.DebuffScale);;
        }
        else
        {
            LinuxOverlayHelper.SetClickThrough(handle.Handle, true);
        }
    }
}