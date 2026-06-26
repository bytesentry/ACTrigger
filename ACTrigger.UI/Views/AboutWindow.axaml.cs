using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ACTrigger.UI.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
    }

    private static void OpenUrl(string url)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
    }

    private void Website_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenUrl("https://bytesentry.github.io/");
    }

    private void Coffee_Click(
        object? sender,
        RoutedEventArgs e)
    {
        OpenUrl("https://buymeacoffee.com/bytesentry");
    }
}