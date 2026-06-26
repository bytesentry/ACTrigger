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

    private void GitHub_Click(
        object? sender,
        RoutedEventArgs e)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName =
                    "https://github.com/bytesentry",
                UseShellExecute =
                    true
            });
    }
}