using System;
using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ACTrigger.Models;
using ACTrigger.Core.Services;
using System.Collections.Generic;

namespace ACTrigger.UI.ViewModels;

public partial class TriggerDialogViewModel : ViewModelBase
{
    private readonly SoundService _soundService = new();

    [ObservableProperty]
    private string triggerName = "";

    [ObservableProperty]
    private string pattern = "";

    [ObservableProperty]
    private bool caseSensitive;

    [ObservableProperty]
    private bool startsWith;

    [ObservableProperty]
    private string soundFile = "";
    
    [ObservableProperty]
    private bool ignoreOutgoing;

    public IEnumerable<TriggerChannel> AvailableChannels =>
        Enum.GetValues<TriggerChannel>();

    [ObservableProperty]
    private TriggerChannel channel = TriggerChannel.Any;

    public ObservableCollection<string> AvailableSounds { get; } =
        new();

    public TriggerDialogViewModel()
    {
        if (Directory.Exists("Sounds"))
        {
            foreach (var file in Directory.GetFiles(
                "Sounds",
                "*.wav"))
            {
                AvailableSounds.Add(
                    Path.GetFileName(file));
            }
        }
    }

    [RelayCommand]
    private void TestSound()
    {
        if (string.IsNullOrWhiteSpace(SoundFile))
            return;

        _soundService.Play(
            Path.Combine(
                "Sounds",
                SoundFile));
    }

    public Trigger ToTrigger()
    {
        return new Trigger
        {
            Name = TriggerName,
            Pattern = Pattern,
            CaseSensitive = CaseSensitive,
            StartsWith = StartsWith,
            Channel = Channel,
            IgnoreOutgoing = IgnoreOutgoing,
            SoundFile = string.IsNullOrWhiteSpace(SoundFile)
                ? null
                : Path.Combine(
                    "Sounds",
                    SoundFile)
        };
    }
}