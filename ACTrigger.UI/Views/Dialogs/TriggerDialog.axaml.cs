using Avalonia.Controls;
using ACTrigger.Models;
using ACTrigger.UI.ViewModels;

namespace ACTrigger.UI.Views.Dialogs;

public partial class TriggerDialog : Window
{
    public Trigger? Result { get; private set; }

    public TriggerDialogViewModel? ViewModel { get; }

    public TriggerDialog()
    {
        InitializeComponent();
    }

    public TriggerDialog(
        TriggerDialogViewModel viewModel)
        : this()
    {
        ViewModel = viewModel;

        DataContext = viewModel;

        SaveButton.Click += OnSaveClicked;
        CancelButton.Click += OnCancelClicked;
    }

    private void OnSaveClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Result = ViewModel!.ToTrigger();

        Close();
    }

    private void OnCancelClicked(
        object? sender,
        Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }
}