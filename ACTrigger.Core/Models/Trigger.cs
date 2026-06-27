using CommunityToolkit.Mvvm.ComponentModel;

namespace ACTrigger.Models;

public partial class Trigger : ObservableObject
{
    [ObservableProperty]
    private string name = "";

    [ObservableProperty]
    private string pattern = "";

    [ObservableProperty]
    private bool enabled = true;

    [ObservableProperty]
    private bool caseSensitive;

    [ObservableProperty]
    private bool startsWith;

    [ObservableProperty]
    private string? soundFile;
}