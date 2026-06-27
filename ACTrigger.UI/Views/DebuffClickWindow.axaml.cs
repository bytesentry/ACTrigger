using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ACTrigger.UI.Models;

namespace ACTrigger.UI.Views;

public partial class DebuffClickWindow : Window
{
    public DebuffClickWindow()
    {
        InitializeComponent();

    }

    private void TargetButton_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not DebuffGroup group)
            return;

    }
}