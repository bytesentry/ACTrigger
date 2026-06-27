using System;
using System.Collections.ObjectModel;
using ACTrigger.Core.Services;
using ACTrigger.Models;
using CommunityToolkit.Mvvm.Input;
using ACTrigger.UI.Views.Dialogs;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using System.Threading.Tasks;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using ACTrigger.UI.Models;

namespace ACTrigger.UI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly SoundService _soundService = new();
    private readonly TriggerService _triggerService = new();
    private readonly SettingsService _settingsService = new();
    private readonly TriggerConfigService _triggerConfigService = new();  
    [ObservableProperty]
    private bool showEventLog;

    [ObservableProperty]
    private bool overlayEnabled;

    [ObservableProperty]
    private bool editOverlayLayout = true;

    [ObservableProperty]
    private bool showDamageOut;

    [ObservableProperty]
    private bool showDamageIn;

    public ObservableCollection<string> RecentEvents { get; } = new();

    public ObservableCollection<Trigger> Triggers { get; } = new();

    public ObservableCollection<CombatText> OutgoingCombatTexts { get; } = new();

    public ObservableCollection<CombatText> IncomingCombatTexts { get; } = new();
    
    public ObservableCollection<CombatText> KillCombatTexts { get; } = new();

    public ObservableCollection<TrackedDebuff> TrackedDebuffs { get; } = new();

    private double _debuffScale = 1.0;

    public double DebuffScale
    {
        get => _debuffScale;
        set => SetProperty(ref _debuffScale, value);
    }

    private string _selectedDebuffScale = "100%";

    public string[] DebuffScaleOptions =>
    [
        "100%",
        "125%",
        "150%"
    ];
    public string SelectedDebuffScale
    {
        get => _selectedDebuffScale;
        set
        {
            if (SetProperty(
                ref _selectedDebuffScale,
                value))
            {
                DebuffScale = value switch
                {
                    "125%" => 1.25,
                    "150%" => 1.50,
                    _ => 1.00
                };
            }
        }
    }
    
    public List<DebuffGroup> DebuffGroups =>
    TrackedDebuffs
        .GroupBy(
            x => x.TargetId)
        .Select(
            g => new DebuffGroup
            {
                TargetId =
                    g.Key,

                TargetName =
                    g.First().TargetName,

                Debuffs =
                    g.OrderBy(
                        x => x.SpellName)
                    .ToList()
            })
        .OrderBy(
            x => x.TargetName)
        .ToList();

    private bool _sessionActive;
    public bool SessionActive
    {
        get => _sessionActive;
    }
    
    private int _outgoingBurstIndex;

    private int _incomingBurstIndex;

    //private int _killBurstIndex;

    //private int _effectBurstIndex;

    //private DateTime _lastEffectText;

    private DateTime _lastKill;

    private DateTime _lastOutgoingHit;

    private DateTime _lastIncomingHit;
    
    private string GetLogFilePath()
    {
        return Path.Combine(
            LogPath,
            "actrigger.log");
    }

    private double? _lastEnchantAdjustment;



    [ObservableProperty]
    private string logPath = "";
    
    private LogWatcher? _watcher;

    private enum CombatEventType
    {
        None,
        OutgoingDamage,
        IncomingDamage,
        Healing,
        Kill
    }

    private string _pluginStatus = "Plugin status unknown";

    public string PluginStatus
    {
        get => _pluginStatus;
        set => SetProperty(ref _pluginStatus, value);
    }

    private static readonly Regex[] KillPatterns =
    {
        new(@"^You flatten .+'s body with the force of your assault!$", RegexOptions.IgnoreCase),
        new(@"^You bring .+ to a fiery end!$", RegexOptions.IgnoreCase),
        new(@"^You beat .+ to a lifeless pulp!$", RegexOptions.IgnoreCase),
        new(@"^You smite .+ mightily!$", RegexOptions.IgnoreCase),
        new(@"^You obliterate .+!$", RegexOptions.IgnoreCase),
        new(@"^You run .+ through!$", RegexOptions.IgnoreCase),
        new(@"^You reduce .+ to a sizzling, oozing mass!$", RegexOptions.IgnoreCase),
        new(@"^You knock .+ into next Morningthaw!$", RegexOptions.IgnoreCase),
        new(@"^You split .+ apart!$", RegexOptions.IgnoreCase),
        new(@"^You cleave .+ in twain!$", RegexOptions.IgnoreCase),
        new(@"^You slay .+ viciously enough to impart death several times over!$", RegexOptions.IgnoreCase),
        new(@"^You reduce .+ to a drained, twisted corpse!$", RegexOptions.IgnoreCase),

        new(@"^Your killing blow nearly turns .+ inside-out!$", RegexOptions.IgnoreCase),
        new(@"^Your attack stops .+ cold!$", RegexOptions.IgnoreCase),
        new(@"^Your lightning coruscates over .+'s mortal remains!$", RegexOptions.IgnoreCase),
        new(@"^Your assault sends .+ to an icy death!$", RegexOptions.IgnoreCase),

        new(@"^You killed .+!$", RegexOptions.IgnoreCase),

        new(@"^The thunder of crushing .+ is followed by the deafening silence of death!$", RegexOptions.IgnoreCase),
        new(@"^The deadly force of your attack is so strong that .+'s ancestors feel it!$", RegexOptions.IgnoreCase),

        new(@"^.+'s seared corpse smolders before you!$", RegexOptions.IgnoreCase),
        new(@"^.+ is reduced to cinders!$", RegexOptions.IgnoreCase),
        new(@"^.+ is shattered by your assault!$", RegexOptions.IgnoreCase),
        new(@"^.+ catches your attack, with dire consequences!$", RegexOptions.IgnoreCase),
        new(@"^.+ is utterly destroyed by your attack!$", RegexOptions.IgnoreCase),
        new(@"^.+ suffers a frozen fate!$", RegexOptions.IgnoreCase),
        new(@"^.+'s perforated corpse falls before you!$", RegexOptions.IgnoreCase),
        new(@"^.+ is fatally punctured!$", RegexOptions.IgnoreCase),
        new(@"^.+'s death is preceded by a sharp, stabbing pain!$", RegexOptions.IgnoreCase),
        new(@"^.+ is torn to ribbons by your assault!$", RegexOptions.IgnoreCase),
        new(@"^.+ is liquified by your attack!$", RegexOptions.IgnoreCase),
        new(@"^.+'s last strength dissolves before you!$", RegexOptions.IgnoreCase),
        new(@"^Electricity tears .+ apart!$", RegexOptions.IgnoreCase),
        new(@"^Blistered by lightning, .+ falls!$", RegexOptions.IgnoreCase),
        new(@"^.+'s last strength withers before you!$", RegexOptions.IgnoreCase),
        new(@"^.+ is dessicated by your attack!$", RegexOptions.IgnoreCase),
        new(@"^.+ is incinerated by your assault!$", RegexOptions.IgnoreCase)
    };

    private static readonly Regex[] IncomingDamagePatterns =
    {
        new(
            @"^Critical hit! Overpower! .+ \w+ you for (\d+) point.* with .+$",
            RegexOptions.IgnoreCase),

        new(
            @"^Critical hit! .+ \w+ you for (\d+) point.* with .+$",
            RegexOptions.IgnoreCase),

        new(
            @"^Overpower! .+ \w+ you for (\d+) point.* with .+$",
            RegexOptions.IgnoreCase),

        new(
            @"^.+ \w+ you for (\d+) point.* with .+$",
            RegexOptions.IgnoreCase),

        new(
            @"^Magical energies lose (\d+) point.* of health due to .+$",
            RegexOptions.IgnoreCase),

        new(
            @"^You lose (\d+) point.* of health due to .+$",
            RegexOptions.IgnoreCase),

        new(
            @"^.+ casts .+ and drains (\d+) point.*$",
            RegexOptions.IgnoreCase),
        new(
            @"^Critical hit! Overpower! .+ \w+ your .+ for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^Critical hit! .+ \w+ your .+ for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^Overpower! .+ \w+ your .+ for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^.+ \w+ your .+ for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase)
    };

    private static readonly Regex[] OutgoingMagicPatterns =
    {
        // Physical / missile / elemental weapon damage
        new(
            @"^Critical hit!\s+You \w+ .* for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^You \w+ .* for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^Critical hit!\s+Sneak Attack!\s+You \w+ .* for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^Sneak Attack!\s+You \w+ .* for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^Sneak Attack!\s+Recklessness!\s+You \w+ .* for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        new(
            @"^Recklessness!\s+You \w+ .* for (\d+) point.* of .+ damage.*$",
            RegexOptions.IgnoreCase),

        // Spell damage (Flame Bolt, Acid Stream, Lightning Bolt, etc.)
        new(
            @"^Critical hit!\s+You .+ for (\d+) point.* with .+\.$",
            RegexOptions.IgnoreCase),

        new(
            @"^You .+ for (\d+) point.* with .+\.$",
            RegexOptions.IgnoreCase)
    };

    private static readonly Regex[] HealingPatterns =
    {
        new(
            @"^You .*heal yourself for (\d+) Health points\.",
            RegexOptions.IgnoreCase),

        new(
            @"^You receive (\d+) points of periodic healing\.$",
            RegexOptions.IgnoreCase),

        new(
            @"^.+ restore(?:s)? (\d+) points of your health\.$",
            RegexOptions.IgnoreCase)
    };

    private static readonly string[] DebuffKeywords =
    {
        "Vulnerability",
        "Imperil",
        "Frailty",
        "Defenselessness",
        "Corruption",
        "Weakness",
        "Slowness",
        "Brittlemail"
    };

    private static readonly Dictionary<string, string> DebuffTranslations =
        new()
        {
            // Armor
            ["Imperil Other"] = "Armor",
            ["Gossamer Flesh"] = "Armor",
            

            // Attributes (1-6 and 8)
            ["Strength Frailty"] = "Strength",
            ["Endurance Frailty"] = "Endurance",
            ["Coordination Frailty"] = "Coordination",
            ["Quickness Frailty"] = "Quickness",
            ["Focus Frailty"] = "Focus",
            ["Self Frailty"] = "Self",
            ["Willpower Frailty"] = "Willpower",

            // Attribute VII versions
            ["Frailty"] = "Strength",
            ["Exhaustion"] = "Endurance",
            ["Bafflement"] = "Coordination",
            ["Clumsiness"] = "Quickness",
            ["Feeblemind"] = "Focus",
            ["Foolproof"] = "Self",
            ["Lethargy"] = "Willpower",

            // Magic skills
            ["Arcane Benightedness"] = "Life Magic",
            ["Magic Yield"] = "War Magic",
            ["Creature Enchantment Ineptitude"] = "Creature Magic",
            ["Item Enchantment Ineptitude"] = "Item Magic",
            ["Void Magic Ineptitude"] = "Void Magic",
            ["Healing Ineptitude"] = "Healing",
            ["Fester"] = "Healing Rate",

            // Skill VII versions
            ["Eradicate Life Magic"] = "Life Magic",
            ["Eradicate War Magic"] = "War Magic",
            ["Eradicate Creature Magic"] = "Creature Magic",
            ["Eradicate Item Magic"] = "Item Magic",
            ["Eradicate Void Magic"] = "Void Magic",

            // Elemental vulnerabilities
            ["Lightning Vulnerability"] = "Lightning",
            ["Fire Vulnerability"] = "Fire",
            ["Cold Vulnerability"] = "Cold",
            ["Acid Vulnerability"] = "Acid",
            ["Olthoi's Gift"] = "Acid",
            ["Inferno's Gift"] = "Fire",     
            ["Astyrrian's Gift"] = "Lightning",
            ["Gelidite's Gift"] = "Cold",
            

            // Physical vulnerabilities
            ["Blade Vulnerability"] = "Slash",
            ["Swordsman's Gift"] = "Slash",
            ["Piercing Vulnerability"] = "Pierce",
            ["Archer's Gift"] = "Pierce",
            ["Bludgeoning Vulnerability"] = "Bludgeon",
            ["Tusker's Gift"] = "Bludgeon",

            // Combat skills
            ["Missile Weapon Ineptitude"] = "Missile",
            ["Finesse Weapon Ineptitude"] = "Finesse",
            ["Heavy Weapon Ineptitude"] = "Heavy",
            ["Light Weapon Ineptitude"] = "Light",
            ["Two Handed Combat Ineptitude"] = "Two-Handed",

            // Skill VII combat versions
            ["Missile Weapon Mastery Other"] = "Missile",
            ["Finesse Weapon Mastery Other"] = "Finesse",
            ["Heavy Weapon Mastery Other"] = "Heavy",
            ["Light Weapon Mastery Other"] = "Light",
            ["Two Handed Mastery Other"] = "Two-Handed",

            // Defenses
            ["Futility"] = "Magic Defense",
            ["Magic Yield"] = "Magic Defense",
            ["Broadside of a Barn"] = "Missile Defense",
            ["Defenselessness"] = "Missile Defense",
            // Keep generic catch-alls LAST (basic vulnerability)
            ["Vulnerability"] = "Melee Defense",
            ["Gravity Well"] = "Melee Defense"
        };

    private static readonly int[] XOffsets =
    {
        0,
        -70,
        70,
        -140,
        140,
        -35,
        35,
        0
    };

    private static readonly int[] YOffsets =
    {
        0,
        60,
        60,
        -40,
        -40,
        110,
        110,
        -90
    };
    private static readonly int[] KillXOffsets =
    {
        0,
        -70,
        70,
    -140,
        140,
        -35,
        35,
        0
    };

    private static readonly int[] KillYOffsets =
    {
        35,
        95,
        95,
        -5,
        -5,
        145,
        145,
        -55
    };

    /*
    private static readonly int[] EffectYOffsets =
    {
        0,
        35,
        70,
        105,
        140
    };
    */
    private int _killBurstIndex;

    private readonly List<PendingDebuff> _pendingDebuffs = new();

    [RelayCommand]
    private async Task BrowseLogFile()
    {
        if (App.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var folders =
            await desktop.MainWindow!.StorageProvider
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

        LogPath =
            folders[0].Path.LocalPath;

        SaveSettings();
    }

    public string DisplayLogPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(LogPath))
                return "No log file selected";

            var parts = LogPath.Split(
                Path.DirectorySeparatorChar,
                StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length <= 4)
                return LogPath;

            return "...\\" +
                string.Join(
                    "\\",
                    parts[^4..]);
        }
    }

    partial void OnLogPathChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayLogPath));
        SaveSettings();
    }

    private void SaveSettings()
    {
        var settings =
            _settingsService.Load();

        settings.LogPath =
            LogPath;

        settings.OverlayEnabled =
            OverlayEnabled;

        settings.ShowDamageOut =
            ShowDamageOut;

        settings.ShowDamageIn =
            ShowDamageIn;

        settings.ShowDebuffs =
            ShowDebuffs;

        settings.AllowTargeting =
            AllowTargeting;

        settings.EditOverlayLayout =
            EditOverlayLayout;

        _settingsService.Save(
            settings);
    }

    private bool _showDebuffs = false;
        public bool ShowDebuffs
        {
            get => _showDebuffs;
            set
            {
                if (_showDebuffs == value)
                    return;

                _showDebuffs = value;
                OnPropertyChanged();

                SaveSettings();
            }
        }

        private bool _allowTargeting = false;
        public bool AllowTargeting
        {
            get => _allowTargeting;
            set
            {
                if (_allowTargeting == value)
                    return;

                _allowTargeting = value;
                OnPropertyChanged();

                SaveSettings();
            }
        }
                    
    [ObservableProperty]
    private Trigger? selectedTrigger;

    public void TestSelectedTrigger()
    {
        if (SelectedTrigger == null)
            return;

        if (string.IsNullOrEmpty(
            SelectedTrigger.SoundFile))
            return;

        _soundService.Play(
            SelectedTrigger.SoundFile);
    }

    public MainWindowViewModel()
    {
        var settings = _settingsService.Load();

        OverlayEnabled =
            settings.OverlayEnabled;

        ShowDamageOut =
            settings.ShowDamageOut;

        ShowDamageIn =
            settings.ShowDamageIn;

        EditOverlayLayout =
            settings.EditOverlayLayout;

        ShowDebuffs =
            settings.ShowDebuffs;

        AllowTargeting =
            settings.AllowTargeting;

        LogPath = settings.LogPath;

        var triggerConfig =
            _triggerConfigService.Load();
        
        foreach (var trigger in triggerConfig.Triggers)
        {
            trigger.PropertyChanged += Trigger_PropertyChanged;

            Triggers.Add(trigger);
        }
        
        if (Triggers.Count > 0)
        {
            SelectedTrigger = Triggers[0];
        }
        _triggerService.TriggerMatched += OnTriggerMatched;
        StartLogWatcher();

        var timer =
            new Avalonia.Threading.DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(16)
            };

        timer.Tick +=
            (_, _) => UpdateCombatTextAnimations();

        timer.Start();
    }

    private void UpdateCombatTextAnimations()
    {
        UpdateCollection(
            OutgoingCombatTexts,
            false);

        UpdateCollection(
            IncomingCombatTexts,
            true);

        //UpdateCollection(
        //    KillCombatTexts,
        //    false);

        foreach (var debuff in TrackedDebuffs)
        {
            debuff.RefreshTimer();
        }
    }

    private void UpdateCollection(
        ObservableCollection<CombatText> texts,
        bool incoming)
    {
        double dt = 0.016;

        for (int i = texts.Count - 1;
            i >= 0;
            i--)
        {
            var text =
                texts[i];

            text.Age += dt;

            if (text.Age < 0.3)
            {
                // IMPACT

                double t =
                    text.Age / 0.3;
                
                double fadeT =
                    (text.Age - 0.7) /
                    (text.Lifetime - 0.7);

                fadeT =
                    Math.Clamp(
                        fadeT,
                        0,
                        1);

                text.Opacity =
                    1.0 - fadeT;

                double targetScale =
                    text.Type == CombatTextType.Kill
                        ? 1.6
                        : text.IsCritical
                            ? 1.25
                            : 1.0;

                double scaleT = t * t;

                text.Scale =
                    0.5 +
                    ((targetScale - 0.5) * scaleT);;

                double targetY =
                    incoming
                        ? text.SpawnY + 150
                        : text.SpawnY - 150;

                text.Y =
                    text.SpawnY +
                    ((targetY - text.SpawnY) * t);
            }
            else if (text.Age < 0.7)
            {
                // SHORT SETTLE
                text.Opacity = 1.0;
                double targetScale =
                    text.IsCritical
                        ? 1.25
                        : 1.0;

                text.Scale +=
                    (targetScale -
                    text.Scale) * 0.15;

                if (incoming)
                {
                    text.Y += 0.2;
                }
                else
                {
                    text.Y -= 0.2;
                }
            }
            else
            {
                // DRIFT + FADE

                
                if (incoming)
                {
                    text.Y += 0.6;
                }
                else
                {
                    text.Y -= 0.6;
                }

                text.Scale =
                    Math.Max(
                        0.8,
                        text.Scale - 0.003);

                text.Opacity =
                    Math.Max(
                        0,
                        text.Opacity - 0.015);
            }

            if (text.Age >=
                text.Lifetime)
            {
                texts.RemoveAt(i);
                continue;
            }
        }
    }

    private void Trigger_PropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        SaveTriggers();
    }

    private void OnTriggerMatched(
        Trigger trigger,
        LogEntry entry)
    {
        RecentEvents.Add(
            $"[{DateTime.Now:HH:mm:ss}] {trigger.Name} - {entry.Message}");

        while (RecentEvents.Count > 100)
        {
            RecentEvents.RemoveAt(0);
        }
    }

    [RelayCommand]
    private async Task AddTrigger()
    {
        var vm = new TriggerDialogViewModel();

        var dialog = new TriggerDialog(vm);

        await dialog.ShowDialog(
            App.Current!.ApplicationLifetime
                is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow!
                : throw new InvalidOperationException());

        if (dialog.Result == null)
            return;

        if (string.IsNullOrWhiteSpace(dialog.Result.Pattern))
            return;

        Triggers.Add(dialog.Result);
        dialog.Result.PropertyChanged +=
        Trigger_PropertyChanged;

        SelectedTrigger = dialog.Result;

        SaveTriggers();
    }

    [RelayCommand]
    private void RemoveTrigger()
    {
        if (SelectedTrigger == null)
            return;

        Triggers.Remove(SelectedTrigger);

        SelectedTrigger =
            Triggers.Count > 0
                ? Triggers[0]
                : null;

        SaveTriggers();
    }

    [RelayCommand]
    private async Task EditTrigger()
    {
        if (SelectedTrigger == null)
            return;

        var vm = new TriggerDialogViewModel
        {
            TriggerName = SelectedTrigger.Name,
            Pattern = SelectedTrigger.Pattern,
            CaseSensitive = SelectedTrigger.CaseSensitive,
            StartsWith = SelectedTrigger.StartsWith,
            SoundFile = Path.GetFileName(
                SelectedTrigger.SoundFile ?? "")
        };

        var dialog = new TriggerDialog(vm);

        if (App.Current!.ApplicationLifetime
            is not Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            throw new InvalidOperationException();
        }

        await dialog.ShowDialog(desktop.MainWindow!);

        if (dialog.Result == null)
            return;

        SelectedTrigger.Name = dialog.Result.Name;
        SelectedTrigger.Pattern = dialog.Result.Pattern;
        SelectedTrigger.CaseSensitive = dialog.Result.CaseSensitive;
        SelectedTrigger.StartsWith = dialog.Result.StartsWith;
        SelectedTrigger.SoundFile = dialog.Result.SoundFile;

        SaveTriggers();
    }

    partial void OnOverlayEnabledChanged(bool value)
    {
        SaveOverlaySettings();
    }

    partial void OnShowDamageOutChanged(bool value)
    {

        SaveOverlaySettings();
    }

    partial void OnShowDamageInChanged(bool value)
    {

        SaveOverlaySettings();
    }

    partial void OnEditOverlayLayoutChanged(bool value)
    {
        SaveOverlaySettings();
    }
    
    private void SaveOverlaySettings()
    {
        var settings =
            _settingsService.Load();

        settings.OverlayEnabled =
            OverlayEnabled;

        settings.ShowDamageOut =
            ShowDamageOut;

        settings.ShowDamageIn =
            ShowDamageIn;

        settings.EditOverlayLayout =
            EditOverlayLayout;

        _settingsService.Save(settings);
    }
    private void SaveTriggers()
    {
        var config = new TriggerConfig();

        foreach (var trigger in Triggers)
        {
            config.Triggers.Add(trigger);
        }

        _triggerConfigService.Save(config);
        _triggerService.Reload();
    }

    private async void StartLogWatcher()
    {
        try
        {
            _watcher = new LogWatcher();

            _watcher.LogEntryReceived += OnLogEntry;

            await _watcher.StartAsync(
                GetLogFilePath());
        }
        catch (Exception ex)
        {
            RecentEvents.Insert(
                0,
                $"ERROR: {ex.Message}");
        }
    }

    private static string? ExtractValue(
        string text,
        Regex[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match =
                pattern.Match(text);

            if (match.Success)
            {
                return
                    match.Groups[1].Value;
            }
        }

        return null;
    }

    private void OnLogEntry(LogEntry entry)
    {
        // checking whether session active via log entries
        _triggerService.Check(entry);

        string text = entry.Message;

        if (entry.Channel == "System" &&
            text == "ACTrigger started")
        {
            Console.WriteLine("SESSION ACTIVE");
            _sessionActive = true;
            OnPropertyChanged(nameof(SessionActive));
        }

        if (entry.Channel == "System" &&
            text == "ACTrigger stopped")
        {
            Console.WriteLine("SESSION ENDED");
            _sessionActive = false;
            OnPropertyChanged(nameof(SessionActive));

            ClearAllDebuffs();

            return;
        }

        if (entry.Channel == "PortalComplete")
        {
            ClearAllDebuffs();
            return;
        }

        string originalText = text;

        bool incomingDebuff =
            entry.Channel == "Enchant" &&
            text.StartsWith(
                "Add |",
                StringComparison.OrdinalIgnoreCase) &&
            IsDebuff(
                text);

        var rng = Random.Shared;

        string? incomingDamageValue = null;

        string? outgoingMagicValue = null;

        string? healingValue = null;

        if (entry.Channel == "DebuffCast")
        {
            ProcessDebuffCast(entry.Message);
            return;
        }

        if (
            entry.Channel == "Combat" &&
            text.StartsWith(
                "You cast ",
                StringComparison.Ordinal) &&
            text.Contains(
                " on ",
                StringComparison.Ordinal))
        {
            ProcessSuccessfulDebuff(text);
        }

        //checks for verified release, making sure obj dead or gone
        if (entry.Channel == "ReleaseVerified")
        {
            ProcessReleaseObject(entry.Message);
            return;
        }

        if (entry.Channel == "Combat")
        {
            incomingDamageValue =
                ExtractValue(
                    text,
                    IncomingDamagePatterns);

            outgoingMagicValue =
                ExtractValue(
                    text,
                    OutgoingMagicPatterns);

            healingValue =
                ExtractValue(
                    text,
                    HealingPatterns);
        }
        //make sure no +0 heal spam
        if (int.TryParse(
                healingValue,
                out int healAmount) &&
            healAmount <= 0)
        {
            return;
        }

        if (entry.Channel == "EnchantRaw")
        {
            var match =
                Regex.Match(
                    text,
                    @"Adjustment=([-+]?\d+(\.\d+)?)");

            if (match.Success)
            {
                _lastEnchantAdjustment =
                    double.Parse(
                        match.Groups[1].Value,
                        CultureInfo.InvariantCulture);
            }

            return;
        }

        if (entry.Channel == "Enchant" &&
            text.StartsWith("Add |"))
        {
            var match =
                Regex.Match(
                    text,
                    @"Name=([^|]+)");

            if (match.Success)
            {
                string spellName =
                    match.Groups[1]
                        .Value
                        .Trim();

                string? stat =
                    TranslateDebuff(
                        spellName);
                

                if (stat != null)
                {
                    //Console.WriteLine(
                    //    $"DISPLAYING DEBUFF [{stat}]");

                    AddStatusText(
                        $"- {stat}",
                        "#FF7777");

                    _lastEnchantAdjustment =
                        null;

                    return;
                }
            }
        }

        bool kill =
            KillPatterns.Any(
                pattern =>
                    pattern.IsMatch(text));

        if (kill)
        {
            
            AddKillText(
                entry.Timestamp);

            return;
        }
        

        string display = "";

        if (healingValue != null)
        {
            display = "+" + healingValue;             
        }
        else if (incomingDamageValue != null)
        {
            display =
                incomingDamageValue;
        }
        else if (outgoingMagicValue != null)
        {
            display =
                outgoingMagicValue;

            text =
                $"You spell-hit for {outgoingMagicValue}";
        }

        if (healingValue != null)
        {
            AddIncomingText(
                "+" + healingValue,
                "#66FF66",
                entry.Timestamp);

            return;
        }

        if (incomingDamageValue != null)
        {
            AddIncomingText(
                incomingDamageValue,
                "#FFFFFF",
                entry.Timestamp);

            return;
        }
        
        if (!kill &&
            entry.Channel != "Combat")
        {
            return;
        }

        

        bool outgoing =
            Regex.IsMatch(
                text,
                @"^(Critical hit!\s+)?(Sneak Attack!\s+)?(Recklessness!\s+)?You .* for \d+ point");

       

        if (outgoingMagicValue != null)
        {
            outgoing = true;
        }
        
        string color;

        if (text.Contains("Critical hit!"))
        {
            color = "#FF9C00";
        }
        else if (outgoing)
        {
            color = "#FFE45C";
        }
        else
        {
            color = "#FFFFFF";
        }

        if (!outgoing &&
            incomingDamageValue == null &&
            healingValue == null)
        {
            return;
        }

        bool critical =
            originalText.Contains(
                "Critical hit!",
                StringComparison.OrdinalIgnoreCase);

        if (outgoing)
        {
            AddOutgoingText(
                display,
                color,
                entry.Timestamp,
                critical);
        }
        else
        {
            AddIncomingText(
                display,
                color,
                entry.Timestamp,
                critical);
        }
    }
    
    [RelayCommand]
    private void ToggleOverlayLayout()
    {
        EditOverlayLayout =
            !EditOverlayLayout;
    }

    public string EventLogButtonText =>
    ShowEventLog
        ? "▼ Recent Events"
        : "▶ Recent Events";

    [RelayCommand]
    private void ToggleEventLog()
    {
        ShowEventLog = !ShowEventLog;

        OnPropertyChanged(
            nameof(EventLogButtonText));
    }

    private void AddIncomingText(
        string text,
        string color,
        DateTime timestamp,
        bool critical = false)
    {
       
        var rng = Random.Shared;

        var combatText =
            new CombatText
            {
                Text = text,
                Type =
                    CombatTextType.IncomingDamage,

                Color = color,
                Lifetime = 2.0,
                Scale = 0.25,
                Age = 0
            };
        combatText.FontSize = 45;
        if (critical)
        {
            combatText.FontSize = 52;
        }

        if (timestamp -
            _lastIncomingHit >
            TimeSpan.FromMilliseconds(750))
        {
            _incomingBurstIndex = 0;
        }

        _lastIncomingHit =
            timestamp;

        int slot =
            _incomingBurstIndex++ %
            XOffsets.Length;

        combatText.X =
            175 +
            XOffsets[slot];

        combatText.Y =
            rng.Next(
                10,
                50);

        combatText.Y +=
            YOffsets[slot];

        combatText.SpawnX =
            combatText.X;

        combatText.SpawnY =
            combatText.Y;

        //Console.WriteLine(
        //    $"INCOMING CREATE: " +
        //    $"Text={combatText.Text} " +
        //    $"SpawnY={combatText.SpawnY:0.0} " +
        //     $"Y={combatText.Y:0.0} " +
        //    $"Lifetime={combatText.Lifetime:0.0}");

        IncomingCombatTexts.Add(
            combatText);
    }

    private void AddOutgoingText(
        string text,
        string color,
        DateTime timestamp,
        bool critical = false)
    {
       
        var rng = Random.Shared;

        var combatText =
            new CombatText
            {
                Text = text,
                Type =
                    CombatTextType.OutgoingDamage,

                Color = color,

                Lifetime = 2.0,
                Scale = 0.25,
                Age = 0
            };
        
        if (critical)
        {
            combatText.FontSize = 50;
            combatText.Color = "#FF9C00";
        }

        if (timestamp -
            _lastOutgoingHit >
            TimeSpan.FromMilliseconds(400))
        {
            _outgoingBurstIndex = 0;
        }

        _lastOutgoingHit = timestamp;

        int slot =
            _outgoingBurstIndex++ %
            XOffsets.Length;

        combatText.X =
            175 +
            XOffsets[slot];

        combatText.Y =
            rng.Next(
                250,
                280);

        combatText.Y +=
            YOffsets[slot];

        combatText.SpawnX =
            combatText.X;

        combatText.SpawnY =
            combatText.Y;


        OutgoingCombatTexts.Add(
            combatText);
    }

    private void AddKillText(
        DateTime timestamp)
    {        
        var killText =
            new CombatText
            {
                Text = "",
                Type = CombatTextType.Kill,
                Color = "#FF7A00",
                FontSize = 58,
                IsCritical = true,
                Lifetime = 2.0,
                Scale = 0.25,
                Age = 0
            };
        if (timestamp -
            _lastKill >
            TimeSpan.FromMilliseconds(750))
        {
            _killBurstIndex = 0;
        }

        _lastKill =
            timestamp;

        int slot =
            _killBurstIndex++ %
            KillXOffsets.Length;

        killText.X =
            175 +
            KillXOffsets[slot];

        killText.Y =
            280 +
            KillYOffsets[slot];

        killText.SpawnX =
            killText.X;

        killText.SpawnY =
            killText.Y;

        OutgoingCombatTexts.Add(
            killText);
    }

    private static bool IsDebuff(
        string text)
    {
        return DebuffKeywords.Any(
            keyword =>
                text.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string? TranslateDebuff(
        string spellName)
    {
        foreach (var kvp in DebuffTranslations)
        {
            if (Regex.IsMatch(
                spellName,
                $@"\b{Regex.Escape(kvp.Key)}\b",
                RegexOptions.IgnoreCase))
            {
                //Console.WriteLine(
                //    $"MATCH [{kvp.Key}] -> [{kvp.Value}]");

                return kvp.Value;
            }
        }

        return null;
    }
    

    private void AddStatusText(
        string text,
        string color)
    {
        IncomingCombatTexts.Add(
            new CombatText
            {
                Text = text,
                Type =
                    CombatTextType.IncomingDamage,

                Color = color,

                FontSize = 25,
                Scale = 0.05,

                Lifetime = 4.0,
                Age = 0,

                X = 175,
                Y = 120,

                SpawnX = 175,
                SpawnY = 120
            });
    }

    private void ProcessDebuffCast(string text)
    {        
        var spellMatch =
            Regex.Match(
                text,
                @"Spell=(.*?)\s+\|");

        var idMatch =
            Regex.Match(
                text,
                @"TargetId=(-?\d+)");

        var nameMatch =
            Regex.Match(
                text,
                @"TargetName=(.+)$");

        var durationMatch =
            Regex.Match(
                text,
                @"Duration=(\d+)");

        var descriptionMatch =
            Regex.Match(
                text,
                @"Description=(.*?)\s+\|");
                

        if (
            !spellMatch.Success ||
            !durationMatch.Success ||
            !descriptionMatch.Success ||
            !idMatch.Success ||
            !nameMatch.Success)
        {
            return;
        }

        int targetId =
            int.Parse(
                idMatch.Groups[1].Value);

        double durationSeconds =
            double.Parse(
                durationMatch.Groups[1].Value);

        string description =
            descriptionMatch.Groups[1].Value.Trim();

        string spellName =
            NormalizeDebuffName(
                spellMatch.Groups[1].Value.Trim());

        string targetName =
            nameMatch.Groups[1].Value.Trim();



        _pendingDebuffs.RemoveAll(
            x =>
                x.TargetId == targetId &&
                x.SpellName == spellName);

        _pendingDebuffs.Add(
            new PendingDebuff
            {
                TargetId = targetId,
                TargetName = targetName,
                SpellName = spellName,
                Description = description,
                DurationSeconds = durationSeconds
            });
        
    }

    private string NormalizeDebuffName(
        string spellName)
    {
        spellName =
            spellName.Replace(
                "Incantation of ",
                "");

        spellName =
            Regex.Replace(
                spellName,
                @"\s+(I|II|III|IV|V|VI|VII|VIII)$",
                "");

        spellName =
            spellName.Replace(
                "Other",
                "");

        return spellName.Trim();
    }

    private void ProcessReleaseObject(string text)
    {
        var match = Regex.Match(text, @"Id=(-?\d+)");

        if (!match.Success)
            return;

        int targetId = int.Parse(match.Groups[1].Value);

        var matches = TrackedDebuffs
            .Where(x => x.TargetId == targetId)
            .ToList();

        foreach (var debuff in matches)
        {
            TrackedDebuffs.Remove(debuff);
        }

        if (matches.Count > 0)
        {
            OnPropertyChanged(nameof(DebuffGroups));
        }
    }

    private void ClearAllDebuffs()
    {
        _pendingDebuffs.Clear();
        TrackedDebuffs.Clear();

        OnPropertyChanged(nameof(DebuffGroups));
    }

    private void ProcessSuccessfulDebuff(
        string text)
    {
        //Console.WriteLine(
        //   $"SUCCESS CHECK: {text}");

        var pending =
            _pendingDebuffs.FirstOrDefault(
                x =>
                    text.Contains(
                        x.SpellName,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    text.Contains(
                        x.TargetName,
                        StringComparison.OrdinalIgnoreCase));

        if (pending == null)
            return;

        var existing =
            TrackedDebuffs.FirstOrDefault(
                x =>
                    x.TargetId == pending.TargetId &&
                    x.SpellName == pending.SpellName);

        if (existing != null)
        {
            existing.ExpiresAt =
                DateTime.UtcNow.AddSeconds(
                    pending.DurationSeconds);

            existing.DurationSeconds =
                pending.DurationSeconds;

            existing.Description =
                pending.Description;

            _pendingDebuffs.Remove(
                pending);

            return;
        }

        TrackedDebuffs.Add(
            new TrackedDebuff
            {
                TargetId = pending.TargetId,
                TargetName = pending.TargetName,
                SpellName = pending.SpellName,
                Description = pending.Description,
                DurationSeconds = pending.DurationSeconds,
                ExpiresAt =
                    DateTime.UtcNow.AddSeconds(
                        pending.DurationSeconds)
            });
        
        OnPropertyChanged(
            nameof(DebuffGroups));

        
        
        _pendingDebuffs.Remove(
            pending);
    }

}