using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ACTrigger.UI.ViewModels;
using System.IO;

namespace ACTrigger.UI.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Opened += (_, _) =>
        {
            UpdatePluginStatus();
        };
    }
    private async void Browse_Click(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders =
            await StorageProvider
                .OpenFolderPickerAsync(
                    new FolderPickerOpenOptions
                    {
                        Title =
                            "Select ACTrigger Decal Plugin Folder",
                        AllowMultiple =
                            false
                    });

        if (folders.Count == 0)
            return;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.LogPath =
                folders[0].Path.LocalPath;

            UpdatePluginStatus();
        }
    }
    

    private void UpdatePluginStatus()
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        var pluginPath =
            Path.Combine(
                vm.LogPath,
                "ACTrigger.Decal.dll");

        vm.PluginStatus =
            File.Exists(pluginPath)
                ? "✓ ACTrigger.Decal.dll Found"
                : "⚠ ACTrigger.Decal.dll Missing";
    }

    private void Done_Click(
    object? sender,
    Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}