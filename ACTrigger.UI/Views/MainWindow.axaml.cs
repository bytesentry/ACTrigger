using System.Collections.Specialized;
using Avalonia.Controls;
using ACTrigger.UI.ViewModels;
using System;
using Avalonia.Threading;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia;
using System.ComponentModel;

namespace ACTrigger.UI.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(
        object? sender,
        EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
            return;

        vm.RecentEvents.CollectionChanged +=
            OnRecentEventsChanged;

        vm.PropertyChanged +=
            Vm_PropertyChanged;
    }

    private async void OnRecentEventsChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (RecentEventsList == null)
            return;

        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Delay(50);

            if (RecentEventsList.ItemCount > 0)
            {
                RecentEventsList.ScrollIntoView(
                    RecentEventsList.ItemCount - 1);
            }
        });
    }

    private void Settings_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var window =
            new SettingsWindow
            {
                DataContext = DataContext
            };

        window.Show(this);
    }
    
    private void About_Click(
        object? sender,
        RoutedEventArgs e)
    {
        var about = new AboutWindow();

        about.Show(this);
    }

    private void Vm_PropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName ==
            nameof(MainWindowViewModel.ShowEventLog))
        {
            InvalidateMeasure();
            InvalidateArrange();

            SizeToContent =
                SizeToContent.Manual;

            SizeToContent =
                SizeToContent.Height;
        }
    }
}