using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using Decal.Interop.Core;
using Decal.Interop.D3DService;

using VirindiViewService;
using VirindiViewService.Controls;


namespace ACTrigger.Decal
{
    public class Plugin : PluginBase
    {
        // -----------------------------------------------------------------------------
        // Services / Files
        // -----------------------------------------------------------------------------

        private FileService? _fileService;
        private System.Timers.Timer? _schedulerTimer;
        private System.Timers.Timer? _logFlushTimer;

        private string? pluginFolder;
        private string? logFile;
        private string? errorLogFile;

        // -----------------------------------------------------------------------------
        // Logging
        // -----------------------------------------------------------------------------

        private readonly ConcurrentQueue<string> _logQueue = new();
        private readonly object _logLock = new();

        // -----------------------------------------------------------------------------
        // HUD Configuration
        // -----------------------------------------------------------------------------

        private bool _showSelfNameplate = false;

        private const float NameplateScale = 0.34f;
        private const float TagScale = NameplateScale * 0.67f;
        private const float LevelScale = TagScale;

        private const float CenterBottomOffset = 0.25f;
        private const float TagOffset = 0.00f;

        // -----------------------------------------------------------------------------
        // HUD State
        // -----------------------------------------------------------------------------

        private readonly Dictionary<int, HudState> _hudStates = new();
        private readonly Dictionary<int, D3DObj> _nameplates = new();

        private readonly object _nameplateLock = new();

        private long _nextHiddenOrder;

        // -----------------------------------------------------------------------------
        // World Tracking
        // -----------------------------------------------------------------------------

        private readonly HashSet<int> trackedTargets = new();
        private readonly HashSet<int> _requestedObjectIds = new();
        private readonly HashSet<int> _awaitingHud = new();

        private readonly Dictionary<int, DateTime> pendingReleases = new();
        private readonly Dictionary<int, DateTime> _pendingRemovalChecks = new();

        private readonly object _trackedTargetsLock = new();
        private readonly object _requestedObjectIdsLock = new();
        private readonly object pendingReleaseLock = new();
        private readonly object _awaitingHudLock = new();

        // -----------------------------------------------------------------------------
        // Render Queues
        // -----------------------------------------------------------------------------

        //private readonly Queue<int> _pendingCreates = new();
        private readonly Queue<PendingCreate> _pendingCreates = new();

        private readonly Queue<int> _pendingVisibility = new();
        private readonly Queue<int> _pendingDisposes = new();

        private readonly HashSet<int> _visibilityQueued = new();

        private readonly object _renderPendingLock = new();

        // -----------------------------------------------------------------------------
        // Timer Queues
        // -----------------------------------------------------------------------------

        private readonly Queue<int> _pendingObjects = new();
        private readonly HashSet<int> _pendingObjectSet = new();

        private readonly object _pendingObjectLock = new();

        // -----------------------------------------------------------------------------
        // Render limits
        // -----------------------------------------------------------------------------

        private const int MaxCreatesPerFrame = 3;
        private const int MaxDisposesPerFrame = 10;
        private const int MaxDirtyPerFrame = 10;
        private const int MaxVisibilityPerFrame = 10;

        // -----------------------------------------------------------------------------
        // HUD Cache / Maintenance
        // -----------------------------------------------------------------------------

        private volatile bool _hudsEnabled = true;
        private const int CacheResetIntervalTicks = 2400;

        //private const int FullResetIntervalTicks = 3500;
        //private int _fullResetCounter;
        //private bool _resetAllPending;

        private const int HudPruneCheckIntervalTicks = 180;
        private const int MaxHudCache = 500;
        private const int TargetHudCache = 300;

        private int _cleanupCounter;
        private int _cacheResetCounter;
        private int _processAwaitingCounter;
        //private int _reconcileCounter;

        private int _hudPruneCounter;

        private volatile bool _refreshHudsRequested;
        private volatile bool _pruneHudsRequested;
        

        // -----------------------------------------------------------------------------
        // Timer State
        // -----------------------------------------------------------------------------

        private int _timerRunning;
        private int _visibilityCounter;
        private int _dpsCounter;

        private DateTime _nextHudUpdate = DateTime.MinValue;

        private readonly Stopwatch _hudStopwatch = Stopwatch.StartNew();

        // -----------------------------------------------------------------------------
        // DPS
        // -----------------------------------------------------------------------------

        private readonly CombatTracker _combatTracker = new();
        private readonly DpsWindow _dpsWindow = new();

        private bool _dpsEnabled;
        private int _combatTimeoutSeconds = 7;

        public int MaxHit { get; private set; }
        public int CriticalHits { get; private set; }


        // -----------------------------------------------------------------------------
        // TEMP DEBUG (REMOVE AFTER STUTTER INVESTIGATION)
        // -----------------------------------------------------------------------------

        ///////////////////////////////////////////////////////////////////////
        ///////////////////////////////////////////////////////////////////////

        protected override void Startup()
        {
            pluginFolder = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            logFile = System.IO.Path.Combine(pluginFolder, "actrigger.log");

            errorLogFile = System.IO.Path.Combine(pluginFolder, "actrigger-errors.log");


            if (!System.IO.File.Exists(logFile))
                System.IO.File.WriteAllText(logFile, "");

            try
            {
                _fileService =
                    CoreManager.Current.Filter<FileService>();


                var spell =
                    _fileService.SpellTable.GetById(2183);

            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(CharacterFilter_SpellCast),
                    ex);
            }

            CoreManager.Current.WorldFilter.CreateObject += WorldFilter_CreateObject;
                
            CoreManager.Current.WorldFilter.ReleaseObject += WorldFilter_ReleaseObject;
                
            CoreManager.Current.WorldFilter.ChangeObject += WorldFilter_ChangeObject;
                
            CoreManager.Current.CharacterFilter.SpellCast += CharacterFilter_SpellCast;
                
            CoreManager.Current.CharacterFilter.ChangePortalMode += CharacterFilter_ChangePortalMode;    

            CoreManager.Current.CharacterFilter.ChangeEnchantments += CharacterFilter_ChangeEnchantments;           

            CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
                
            CoreManager.Current.RenderFrame += Core_RenderFrame;

            CoreManager.Current.CommandLineText += Current_CommandLineText;
              
            // check for target provided by file
            
            _schedulerTimer = new System.Timers.Timer(250);
            _schedulerTimer.AutoReset = true;
            _schedulerTimer.Elapsed += (_, _) => SchedulerTick();
            _schedulerTimer.Start();

            _logFlushTimer = new System.Timers.Timer(50);
            _logFlushTimer.AutoReset = true;
            _logFlushTimer.Elapsed += (_, _) => LogFlushTick();
            _logFlushTimer.Start();


             Task.Run(async () =>
            {
                await Task.Delay(5000);

                WriteEvent("System", "ACTrigger started");
                Chat("ACTrigger is active.");
            });
                      
        }

        protected override void Shutdown()
        {
            try
            {

                _schedulerTimer?.Stop();
                _schedulerTimer?.Dispose();
                _logFlushTimer?.Stop();
                _logFlushTimer?.Dispose();

                CoreManager.Current.WorldFilter.CreateObject -= WorldFilter_CreateObject;
                    
                CoreManager.Current.WorldFilter.ReleaseObject -= WorldFilter_ReleaseObject;
                    
                CoreManager.Current.WorldFilter.ChangeObject -= WorldFilter_ChangeObject;
                    
                CoreManager.Current.CharacterFilter.ChangeEnchantments -= CharacterFilter_ChangeEnchantments;
                    
                CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
                    
                CoreManager.Current.CharacterFilter.SpellCast -= CharacterFilter_SpellCast;
                    
                CoreManager.Current.CharacterFilter.ChangePortalMode -= CharacterFilter_ChangePortalMode;

                CoreManager.Current.RenderFrame -= Core_RenderFrame;

                CoreManager.Current.CommandLineText -= Current_CommandLineText;

                CleanupDeadHuds();

            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(Shutdown),
                    ex);
            }

            WriteEvent(
                "System",
                "ACTrigger stopped");

            Thread.Sleep(1000);

            File.WriteAllText(logFile, string.Empty);
        }

        private void SchedulerTick()
        {
            if (Interlocked.Exchange(ref _timerRunning, 1) == 1)
                    return;

                
                try
                {
                    CheckTargetRequest();

                    if (_hudsEnabled)
                    {
                        ProcessPendingObjects();

                        _cleanupCounter++;

                        if (_cleanupCounter >= 720)
                        {
                            CleanupDeadHuds();
                            _cleanupCounter = 0;
                        }

                        _visibilityCounter++;

                        if (_visibilityCounter >= 4)
                        {
                            UpdateHuds();
                            _visibilityCounter = 0;
                        }

                        _processAwaitingCounter++;

                        if (_processAwaitingCounter >= 2)
                        {
                            ProcessAwaitingHuds();
                        }

                        _hudPruneCounter++;

                        if (_hudPruneCounter >= HudPruneCheckIntervalTicks)
                        {
                            _hudPruneCounter = 0;

                            bool shouldPrune;

                            lock (_nameplateLock)
                            {
                                shouldPrune = _nameplates.Count > MaxHudCache;
                            }

                            if (shouldPrune)
                            {
                                _pruneHudsRequested = true;
                            }
                        }
                    }

                    ProcessPendingReleases();

                    _dpsCounter++;

                    if (_dpsCounter >= 4)
                    {
                        if (_dpsEnabled)
                        {
                            _combatTracker.Update(_combatTimeoutSeconds);

                            _combatTracker.TickSecond();

                            _dpsWindow.SetStats(
                                _combatTracker.CurrentDps,
                                _combatTracker.TotalDamage,
                                _combatTracker.MaxHit,
                                _combatTracker.CriticalHits,
                                FormatTime(_combatTracker.ElapsedSeconds));
                        }
                        _dpsCounter = 0;
                    }

                    _cacheResetCounter++;

                    if (_cacheResetCounter >= CacheResetIntervalTicks)
                    {
                        ResetRequestedObjectIds();
                        _cacheResetCounter = 0;
                    }
                    
                }
                catch (Exception ex)
                {
                    WriteError(
                        "schedulerTimer",
                        ex);
                }
                finally
                {
                    Interlocked.Exchange(ref _timerRunning, 0);
                }
        }

        private int _logTimerRunning;

        private void LogFlushTick()
        {
            if (Interlocked.Exchange(ref _logTimerRunning, 1) == 1)
                return;

            try
            {
                FlushLogQueue();
            }
            catch (Exception ex)
            {
                WriteError(nameof(LogFlushTick), ex);
            }
            finally
            {
                Interlocked.Exchange(ref _logTimerRunning, 0);
            }
        }

        private void Chat(
            string message)
        {
            CoreManager.Current.Actions.AddChatText(
                $"<{{ACTrigger}}>: {message}",
                5);
        }

        private void CharacterFilter_ChangeEnchantments(
            object sender,
            ChangeEnchantmentsEventArgs e)
        {
            var ench = e.Enchantment;

            WriteEvent(
                "EnchantRaw",
                $"SpellId={ench.SpellId}" +
                $" | Family={ench.Family}" +
                $" | Layer={ench.Layer}" +
                $" | Adjustment={ench.Adjustment}");

            try
            {
                var spell = _fileService?.SpellTable.GetById(ench.SpellId);


                if (spell != null)
                {
                    WriteEvent(
                        "SpellInfo",
                        $"Name={spell.Name}" +
                        $" | School={spell.School}" +
                        $" | Flags={spell.Flags}");
                }

                WriteEvent(
                    "Enchant",
                    $"{e.Type}" +
                    $" | Name={spell?.Name}" +
                    $" | School={spell?.School}" +
                    $" | Family={ench.Family}" +
                    $" | Layer={ench.Layer}" +
                    $" | Duration={ench.Duration}" +
                    $" | Remaining={ench.TimeRemaining}");
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(CharacterFilter_ChangeEnchantments),
                    ex);
            }
        }

        private void Current_ChatBoxMessage(
            object sender,
            ChatTextInterceptEventArgs e)
        {
            try
            {
                string channel = ClassifyChat(e.Text);

                string text = e.Text;

                if (channel != "Chat")
                {
                    text = Regex.Replace(
                        text,
                        @"^\[[^\]]+\]\s*",
                        "");
                }

                if (_dpsEnabled)
                {
                    if (CombatClassifier.TryGetOutgoingDamage(
                            text,
                            out int damage,
                            out bool critical))
                    {
                        _combatTracker.AddDamage(damage, critical);
                    }
                    else if (CombatClassifier.IsOutgoingCombatActivity(text))
                    {
                        _combatTracker.Touch();
                    }
                }

                WriteEvent(
                    channel,
                    text);
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(Current_ChatBoxMessage),
                    ex);
            }
        }

        private string ClassifyChat(string text)
        {
            // Proper chat channels.
            switch (ChatClassifier.GetChatChannel(text))
            {
                case ChatClassifier.ChatChannels.General:
                    return "General";

                case ChatClassifier.ChatChannels.Trade:
                    return "Trade";

                case ChatClassifier.ChatChannels.Fellowship:
                    return "Fellowship";

                case ChatClassifier.ChatChannels.Allegiance:
                    return "Allegiance";

                case ChatClassifier.ChatChannels.LFG:
                    return "LFG";

                case ChatClassifier.ChatChannels.Roleplay:
                    return "Roleplay";

                case ChatClassifier.ChatChannels.Society:
                    return "Society";

                case ChatClassifier.ChatChannels.Area:
                    return "Area";

                case ChatClassifier.ChatChannels.Tell:
                    return "Tell";

                case ChatClassifier.ChatChannels.TellOutgoing:
                    return "TellOutgoing";

                case ChatClassifier.ChatChannels.TellNpc:
                    return "TellNpc";
            }
            // Combat gets first priority.
            if (CombatClassifier.IsCombat(text))
                return "Combat";

            // Spell incantations.
            if (ChatClassifier.IsSpellCastingMessage(text))
                return "SpellCast";

            // Anything we don't recognize is routed or system.
            return EventClassifier.GetEventChannel(text);
        }

        private void WriteEvent(
            string channel,
            string message)
        {
            message = message.Replace(
                Environment.NewLine,
                " ");

            string line =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}|{channel}|{message}\r\n";


            _logQueue.Enqueue(line);
        }

        private void FlushLogQueue()
        {
            if (_logQueue.IsEmpty)
                return;

            var builder = new StringBuilder();

            while (_logQueue.TryDequeue(out string line))
            {
                builder.Append(line);
            }

            if (builder.Length == 0)
                return;

            try
            {
                File.AppendAllText(
                    logFile,
                    builder.ToString());
            }
            catch (Exception ex)
            {
                WriteError(nameof(FlushLogQueue), ex);
            }
        }

        private void WriteError(
            string source,
            Exception ex)
        {
            string text =
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{source}]\r\n" +
                $"{ex}\r\n\r\n";

            lock (_logLock)
            {
                File.AppendAllText(
                    errorLogFile,
                    text);
            }
        }

        private void CharacterFilter_SpellCast(
            object sender,
            SpellCastEventArgs e)
        {
            try
            {
                var spell =
                    _fileService?.SpellTable.GetById(
                        e.SpellId);

                WriteEvent(
                    "SpellCast",
                    $"SpellId={e.SpellId}" +
                    $" | Name={spell?.Name}" +
                    $" | TargetId={e.TargetId}");

                var wo =
                    CoreManager.Current.WorldFilter[e.TargetId];

                if (wo == null)
                {
                    WriteEvent(
                        "SpellCast",
                        "Target lookup returned null");

                    return;
                }
                if (
                    spell?.Name?.Contains("Imperil Other") == true ||
                    spell?.Name == "Tusker's Gift" ||

                    spell?.Name?.Contains("Acid Vulnerability Other") == true ||
                    spell?.Name?.Contains("Fire Vulnerability Other") == true ||
                    spell?.Name?.Contains("Cold Vulnerability Other") == true ||
                    spell?.Name?.Contains("Lightning Vulnerability Other") == true ||
                    spell?.Name == "Astyrrian's Gift" ||
                    spell?.Name == "Inferno's Gift" ||
                    spell?.Name == "Gelidite's Gift" ||
                    spell?.Name == "Olthoi's Gift" ||

                    spell?.Name?.Contains("Blade Vulnerability Other") == true ||
                    spell?.Name?.Contains("Defenselessness") == true ||
                    spell?.Name?.Contains("Bludgeoning Vulnerability Other") == true ||
                    spell?.Name?.Contains("Piercing Vulnerability Other") == true ||
                    spell?.Name?.Contains("Magic Yield") == true ||

                    spell?.Name?.Contains("Feeblemind Other") == true ||

                    spell?.Name?.Contains("Frailty Other") == true ||
                    spell?.Name == "Brittle Bones" ||

                    spell?.Name?.Contains("Clumsiness Other") == true ||
                    spell?.Name == "Broadside of a Barn" ||

                    spell?.Name?.Contains("Slowness Other") == true ||

                    spell?.Name?.Contains("Unfocus Other") == true ||
                    spell?.Name?.Contains("Fester") == true ||

                    spell?.Name?.Contains("Forgetfulness Other") == true ||

                    spell?.Name?.Contains("Creature Enchantment Ineptitude Other") == true ||
                    spell?.Name?.Contains("Item Enchantment Ineptitude Other") == true ||
                    spell?.Name?.Contains("Life Magic Ineptitude Other") == true ||
                    spell?.Name?.Contains("War Magic Ineptitude Other") == true ||

                    spell?.Name?.Contains("Melee Defense Ineptitude Other") == true ||
                    spell?.Name?.Contains("Missile Defense Ineptitude Other") == true ||
                    spell?.Name?.Contains("Magic Defense Ineptitude Other") == true ||

                    spell?.Name == "Vulnerability Other I" ||
                    spell?.Name == "Vulnerability Other II" ||
                    spell?.Name == "Vulnerability Other III" ||
                    spell?.Name == "Vulnerability Other IV" ||
                    spell?.Name == "Vulnerability Other V" ||
                    spell?.Name == "Vulnerability Other VI" ||
                    spell?.Name == "Incantation of Vulnerability Other" ||
                    spell?.Name == "Gravity Well"
                )
                {
                    WriteEvent(
                        "DebuffCast",
                        $"Spell={spell.Name}" +
                        $" | Duration={spell.Duration}" +
                        $" | Description={spell.Description}" +
                        $" | TargetId={wo.Id}" +
                        $" | TargetName={wo.Name}");

                    lock (_trackedTargetsLock)
                    {
                        trackedTargets.Add(wo.Id);
                    }
                }
                WriteEvent(
                    "WorldObject",
                    $"Name={wo.Name}" +
                    $" | Id={wo.Id}" +
                    $" | Class={wo.ObjectClass}");
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(CharacterFilter_SpellCast),
                    ex);
            }
        }

        private void WorldFilter_ReleaseObject(
            object sender,
            ReleaseObjectEventArgs e)
        {
            try
            {
                var wo = e.Released;

                if (wo == null)
                {
                    return;
                }

                if (!ShouldHaveHud(wo) &&
                    wo.ObjectClass != ObjectClass.Corpse)
                {
                    return;
                }

                // Ignore objects we're not actively tracking.
                if (!trackedTargets.Contains(wo.Id))
                {
                    return;
                }

                lock (pendingReleaseLock)
                {
                    pendingReleases[wo.Id] = DateTime.Now.AddSeconds(1);
                }


            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(WorldFilter_ReleaseObject),
                    ex);
            }
        }

        private void Actions_ItemSelected(
            object sender,
            ItemSelectedEventArgs e)
        {
            WriteEvent(
                "ItemSelected",
                e.ToString());
        }

        private string GetTargetRequestPath()
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                "actrigger.target");
        }

        private string GetHudRequestsPath()
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                "actrigger.nameplates");
        }

        //part of debuff overlay
        private void CheckTargetRequest()
        {
            try
            {
                var path =
                    GetTargetRequestPath();

                if (!File.Exists(path))
                    return;

                var text =
                    File.ReadAllText(path)
                        .Trim();

                File.Delete(path);

                if (!int.TryParse(
                        text,
                        out var targetId))
                {
                    WriteEvent(
                        "TargetRequest",
                        $"Invalid ID: {text}");

                    return;
                }

                WriteEvent(
                    "TargetRequest",
                    $"Targeting {targetId}");

                CoreManager.Current.Actions
                    .SelectItem(targetId);
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(CheckTargetRequest),
                    ex);
            }
        }

        // for debuff overlay to check wo removed prematurely (fixinvisible)
        private void ProcessPendingReleases()
        {
            List<KeyValuePair<int, DateTime>> pending;

            lock (pendingReleaseLock)
            {
                if (pendingReleases.Count == 0)
                    return;

                pending = pendingReleases.ToList();
            }

            var now = DateTime.Now;

            foreach (var entry in pending)
            {
                if (entry.Value > now)
                    continue;

                try
                {
                    var lookup = CoreManager.Current.WorldFilter[entry.Key];

                    if (lookup == null)
                    {
                        lock (_trackedTargetsLock)
                        {
                            trackedTargets.Remove(entry.Key);
                        }

                        WriteEvent(
                            "ReleaseVerified",
                            $"Id={entry.Key}");
                    }
                }
                catch (Exception ex)
                {
                    WriteError(
                        nameof(ProcessPendingReleases),
                        ex);
                }

                lock (pendingReleaseLock)
                {
                    pendingReleases.Remove(entry.Key);
                }
            }
        }

        private void CharacterFilter_ChangePortalMode(
            object sender,
            ChangePortalModeEventArgs e)
        {
            //clear playerID to get up-to-date infos
            lock (_requestedObjectIdsLock)
            {
                _requestedObjectIds.Clear();
            }

            if (e.Type == PortalEventType.ExitPortal)
            {
                WriteEvent("PortalComplete", "");
            }
        }

        private void WorldFilter_CreateObject(
            object sender,
            CreateObjectEventArgs e)
        {
            if (!_hudsEnabled)
                return;

            try
            {
                var wo = e.New;

                if (wo == null)
                    return;

                bool shouldHaveHud = ShouldHaveHud(wo);

                if (!shouldHaveHud)
                    return;

                bool needsRequest =
                    wo.ObjectClass == ObjectClass.Player ||
                    wo.ObjectClass == ObjectClass.Monster;


                if (wo.ObjectClass != ObjectClass.Player)
                {
                    if (!string.IsNullOrWhiteSpace(wo.Name))
                    {
                        QueuePendingObject(wo.Id);
                    }
                }

                bool requestId = false;

                // requestID only for player and monster
                if (needsRequest && wo.Id != CoreManager.Current.CharacterFilter.Id)
                {
                    lock (_requestedObjectIdsLock)
                    {
                        requestId = _requestedObjectIds.Add(wo.Id);
                    }
                }

                if (requestId)
                {
                    CoreManager.Current.Actions.RequestId(wo.Id);
                }

                if (string.IsNullOrWhiteSpace(wo.Name))
                    return;
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(WorldFilter_CreateObject),
                    ex);
            }
        }

        private void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e)
        {
            if (!_hudsEnabled)
                return;
            try
            {
                var wo = e.Changed;
                if (wo == null) return;
                if (!ShouldHaveHud(wo)) return;

                
                QueuePendingObject(wo.Id);
            }
            catch (Exception ex)
            {
                WriteError(nameof(WorldFilter_ChangeObject), ex);
            }
        }

        private string GetHudFile(WorldObject wo)     
        {
            return System.IO.Path.Combine(
                pluginFolder,
                "Assets",
                "Cache",
                CoreManager.Current.CharacterFilter.Server,
                "HUD",
                GetHudFolder(wo),
                MakeSafeFilename(wo.Name) + ".png");
        }

        private HudState GetOrCreateHudState(WorldObject wo)
        {
            lock (_nameplateLock)
            {
                if (!_hudStates.TryGetValue(wo.Id, out var hud))
                {
                    hud = new HudState
                    {
                        ObjectId = wo.Id,
                        HiddenOrder = ++_nextHiddenOrder
                    };

                    _hudStates[wo.Id] = hud;
                }

                return hud;
            }
        }

        private static bool SupportsNameplate(WorldObject wo)
        {
            return wo.ObjectClass == ObjectClass.Monster
                || wo.ObjectClass == ObjectClass.Player
                || wo.ObjectClass == ObjectClass.Npc
                || wo.ObjectClass == ObjectClass.Portal
                || wo.ObjectClass == ObjectClass.Vendor;
        }

        private bool ShouldHaveHud(
            WorldObject wo)
        {
            if (!SupportsNameplate(wo))
                return false;

            if (!_showSelfNameplate &&
                wo.ObjectClass == ObjectClass.Player &&
                wo.Id == CoreManager.Current.CharacterFilter.Id)
            {
                return false;
            }

            return true;
        }

        private bool TryGetHudDimensions(
            string file,
            out float width,
            out float height)
        {
            width = 0;
            height = 0;

            try
            {
                string signatureFile =
                    System.IO.Path.ChangeExtension(file, ".hud");

                if (File.Exists(signatureFile))
                {
                    string[] lines =
                        File.ReadAllLines(signatureFile);

                    if (lines.Length >= 2)
                    {
                        string[] parts =
                            lines[1].Split('|');

                        if (parts.Length >= 2 &&
                            float.TryParse(parts[0], out width) &&
                            float.TryParse(parts[1], out height))
                        {
                            return true;
                        }
                    }
                }

                // Fallback for older HUDs that don't have dimensions.
                using (var image = Image.FromFile(file))
                {
                    width = image.Width;
                    height = image.Height;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class PendingCreate
        {
            public int ObjectId;
            public string File = "";
            public float ImageWidth;
            public float ImageHeight;
        }

        private static void MarkHudDirty(HudState hud)
        {
            hud.CacheDirty = true;
            hud.ImageWidth = 0;
            hud.ImageHeight = 0;
        }

        private bool ShowHud(
            WorldObject wo,
            string file,
            float imageWidth,
            float imageHeight)
        {
            lock (_nameplateLock)
            {
                if (_nameplates.ContainsKey(wo.Id))
                    return false;
            }

            var obj = CoreManager.Current.D3DService.NewD3DObj();
            bool registered = false;

            try
            {
                if (!LoadHudImage(obj, file))
                    return false;

                float objectHeight =
                    ((IACHooks)Host.Underlying.Hooks).ObjectHeight(wo.Id);

                ConfigureHud(
                    obj,
                    wo,
                    imageWidth,
                    imageHeight,
                    objectHeight);

                RegisterHud(
                    wo,
                    obj,
                    file);

                registered = true;
                return true;
            }
            catch (Exception ex)
            {
                WriteError(nameof(ShowHud), ex);
                return false;
            }
            finally
            {
                if (!registered)
                {
                    DisposeHud(obj, false);
                }
            }
        }

        private void ConfigureHud(
            D3DObj obj,
            WorldObject wo,
            float imageWidth,
            float imageHeight,
            float objectHeight)
        {
            obj.Anchor(
                wo.Id,
                0.2f,
                0f,
                0f,
                objectHeight + CenterBottomOffset - 0.05f);

            float aspect = imageWidth / imageHeight;

            bool isSingleLine = imageHeight <= 60f;

            float scale;
            float scaleX;

            if (isSingleLine)
            {
                const float SingleLineScale = 0.275f; // 0.283 * 0.97
                const float SingleLineK = 3.1f;

                scale = SingleLineScale;
                scaleX = aspect / SingleLineK;
            }
            else
            {
                const float DoubleLineScale = 0.498f; // 0.513 * 0.97
                const float DoubleLineK = 1.75f;

                scale = DoubleLineScale;
                scaleX = aspect / DoubleLineK;
            }

            const float scaleY = 1f;

            if (ShouldUseSmallNameplate(wo, objectHeight))
            {
                const float SmallNameplateShrinkRatio = 0.498f;
                const float SmallDoubleLine = 4f;

                scale *= SmallNameplateShrinkRatio;
                scaleX = aspect / SmallDoubleLine;
            }

            obj.Scale(scale);
            obj.ScaleX = scaleX;
            obj.ScaleY = scaleY;

            obj.Autoscale = false;
            obj.OrientToCamera(true);
        }

        private bool LoadHudImage(
            D3DObj obj,
            string file)
        {
            try
            {
                var raw = (ID3DObj)obj.Underlying;
                    
                raw.SetIconFromFile(file);

                return true;
            }
            catch (Exception ex)
            {
                WriteEvent(
                    "LoadHudImageFail",
                    $"{System.IO.Path.GetFileName(file)}|{ex.Message}");

                return false;
            }
        }

        private void DisposeHud(
            D3DObj obj,
            bool hide = true)
        {
            if (hide)
            {
                try
                {
                    obj.Visible = false;
                }
                catch
                {
                }
            }

            try
            {
                ((DisposableByRefObject)obj).Dispose();
            }
            catch
            {
            }
        }

        private void RegisterHud(
            WorldObject wo,
            D3DObj obj,
            string file)
        {
            lock (_nameplateLock)
            {
                if (_nameplates.ContainsKey(wo.Id))
                {
                    DisposeHud(obj);
                    return;
                }

                if (_hudStates.TryGetValue(wo.Id, out var hud))
                {
                    obj.Visible = hud.Visible;

                    hud.DisplayedName = hud.DesiredName;
                    hud.DisplayedLevel = hud.DesiredLevel;
                    hud.DisplayedMonarch = hud.DesiredMonarch;

                    hud.RegenerationRequested = false;
                    hud.CacheDirty = false;

                    hud.LastHudWriteTimeUtc =
                        File.GetLastWriteTimeUtc(file);
                }
                else
                {
                    obj.Visible = true;
                }

                _nameplates[wo.Id] = obj;
            }
            lock (_awaitingHudLock)
            {
                _awaitingHud.Remove(wo.Id);
            }
        }

        private static string MakeSafeFilename(string? name)
        {
            name ??= "";
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name;
        }
        
        /*
        private void UpdateHuds()
        {
            try
            {

                const double MaxRenderDistance = 0.25;

                WorldObject player =
                    CoreManager.Current.WorldFilter[
                        CoreManager.Current.CharacterFilter.Id];

                if (player == null)
                    return;

                var playerCoords = player.Coordinates();

                var changedVisibility = new List<int>();

                lock (_nameplateLock)
                {
                    foreach (var pair in _nameplates)
                    {
                        WorldObject wo =
                            CoreManager.Current.WorldFilter[pair.Key];

                        if (wo == null)
                            continue;

                        bool visible =
                            playerCoords.DistanceToCoords(
                                wo.Coordinates()) <= MaxRenderDistance;

                        if (!_hudStates.TryGetValue(pair.Key, out var hud))
                            continue;

                        if (hud.Visible != visible)
                        {
                            hud.Visible = visible;
                            changedVisibility.Add(pair.Key);
                        }
                    }
                }
                if (changedVisibility.Count == 0)
                    return;

                lock (_renderPendingLock)
                {
                    foreach (int id in changedVisibility)
                    {
                        if (_visibilityQueued.Add(id))
                        {
                            _pendingVisibility.Enqueue(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(UpdateHuds),
                    ex);
            }
        }*/
        
        private void UpdateHuds()
        {
            const double MaxRenderDistance = 0.25;

            WorldObject player =
                CoreManager.Current.WorldFilter[
                    CoreManager.Current.CharacterFilter.Id];

            if (player == null)
                return;

            var playerCoords = player.Coordinates();
                
            var changedVisibility = new List<int>();
    
            List<int> ids;

            lock (_nameplateLock)
            {
                ids = _nameplates.Keys.ToList();
            }
            
            foreach (int id in ids)
            {
                WorldObject wo =
                    CoreManager.Current.WorldFilter[id];

                if (wo == null)
                    continue;

                bool visible =
                    playerCoords.DistanceToCoords(
                        wo.Coordinates()) <= MaxRenderDistance;

                lock (_nameplateLock)
                {
                    if (!_hudStates.TryGetValue(id, out var hud))
                        continue;

                    if (hud.Visible != visible)
                    {
                        if (!visible)
                        {
                            hud.HiddenOrder = ++_nextHiddenOrder;
                        }

                        hud.Visible = visible;
                        changedVisibility.Add(id);
                    }
                }
            }
            if (changedVisibility.Count == 0)
                return;

            lock (_renderPendingLock)
            {
                foreach (int id in changedVisibility)
                {
                    if (_visibilityQueued.Add(id))
                    {
                        _pendingVisibility.Enqueue(id);
                    }
                }
            }
        }

        private static bool TryParsePet(
            WorldObject wo,
            out string owner,
            out string petName)
        {
            owner = string.Empty;
            petName = string.Empty;

            const int PetBehaviorFlag = 0x04000000;

            int behavior =
                wo.Values(
                    LongValueKey.Behavior,
                    0);

            if ((behavior & PetBehaviorFlag) == 0)
                return false;

            string name = wo.Name;

            if (string.IsNullOrWhiteSpace(name))
                return false;

            const string marker = "'s ";

            int index = name.IndexOf(
                marker,
                StringComparison.Ordinal);

            if (index <= 0)
                return false;

            owner = name.Substring(0, index).Trim();
            petName = name.Substring(index + marker.Length).Trim();

            return petName.Length > 0;
        }

        private static string GetHudFolder(
            WorldObject wo)
        {
            if (TryParsePet(
                wo,
                out _,
                out _))
            {
                return "Pet";
            }

            switch (wo.ObjectClass)
            {
                case ObjectClass.Monster:
                    return "Monster";

                case ObjectClass.Player:
                    return "Player";

                case ObjectClass.Npc:
                    return "Npc";

                case ObjectClass.Portal:
                    return "Portal";

                case ObjectClass.Vendor:
                    return "Vendor";

                default:
                    return "Monster";
            }
        }

        private void Core_RenderFrame(
            object sender,
            EventArgs e)
        {
            try
            {
                ProcessPendingRenderActions();

                if (_refreshHudsRequested)
                {
                    _refreshHudsRequested = false;
                    ExecuteHudRefresh();
                }

                if (_pruneHudsRequested)
                {
                    _pruneHudsRequested = false;
                    ExecuteHudPrune();
                }
                //if (_resetAllPending)
                //{
                //    ResetAllNameplates();
                //    _resetAllPending = false;
                //}
            }
            catch (Exception ex)
            {
                WriteError(
                    nameof(Core_RenderFrame),
                    ex);
            }
        }

        private void ExecutePendingCreates()
        {
            int processed = 0;

            while (processed < MaxCreatesPerFrame)
            {
                PendingCreate pending;

                lock (_renderPendingLock)
                {
                    if (_pendingCreates.Count == 0)
                        break;

                    pending = _pendingCreates.Dequeue();
                }

                WorldObject wo =
                    CoreManager.Current.WorldFilter[pending.ObjectId];

                if (wo == null)
                {
                    lock (_nameplateLock)
                    {
                        if (_hudStates.TryGetValue(
                                pending.ObjectId,
                                out var hud))
                        {
                            hud.CreateQueued = false;
                        }
                    }

                    continue;
                }

                ShowHud(
                    wo,
                    pending.File,
                    pending.ImageWidth,
                    pending.ImageHeight);

                lock (_nameplateLock)
                {
                    if (_hudStates.TryGetValue(
                            pending.ObjectId,
                            out var hud))
                    {
                        hud.CreateQueued = false;
                    }
                }

                processed++;
            }
        }

        private void ExecutePendingDisposes()
        {
            int processed = 0;

            while (processed < MaxDisposesPerFrame)
            {
                int id;

                lock (_renderPendingLock)
                {
                    if (_pendingDisposes.Count == 0)
                        break;

                    id = _pendingDisposes.Dequeue();
                }

                D3DObj? obj;

                lock (_nameplateLock)
                {
                    if (_hudStates.TryGetValue(id, out var hud))
                        hud.DisposeQueued = false;

                    if (!_nameplates.TryGetValue(id, out obj))
                        continue;

                    _nameplates.Remove(id);
                    _hudStates.Remove(id);
                    _pendingRemovalChecks.Remove(id);
                }

                DisposeHud(obj);
                processed++;
            }

            if (processed > 0)
            {
                int remaining;

                lock (_renderPendingLock)
                {
                    remaining = _pendingDisposes.Count;
                }

                WriteEvent(
                    "DisposeBatch",
                    $"Disposed={processed}, Remaining={remaining}");
            }
        }

        private void ExecutePendingVisibility()
        {
            int processed = 0;

            while (processed < MaxVisibilityPerFrame)
            {
                int id;

                lock (_renderPendingLock)
                {
                    if (_pendingVisibility.Count == 0)
                        break;

                    id = _pendingVisibility.Dequeue();
                    _visibilityQueued.Remove(id);
                }

                D3DObj? obj = null;
                bool visible = false;

                lock (_nameplateLock)
                {
                    _nameplates.TryGetValue(id, out obj);

                    if (_hudStates.TryGetValue(id, out var hud))
                    {
                        visible = hud.Visible;
                    }
                }

                if (obj != null)
                {
                    obj.Visible = visible;
                }

                processed++;
            }
        }

        private void ExecuteHudRefresh()
        {
            int disposed = 0;
            int skipped = 0;

            lock (_renderPendingLock)
            {
                _pendingCreates.Clear();
                _pendingDisposes.Clear();
            }

            lock (_nameplateLock)
            {
                List<int> remove = new();

                foreach (var pair in _nameplates)
                {
                    DisposeHud(pair.Value);
                    remove.Add(pair.Key);
                    disposed++;
                }

                foreach (int objectId in remove)
                {
                    _nameplates.Remove(objectId);
                    _pendingRemovalChecks.Remove(objectId);
                }

                // We intentionally leave _hudStates intact.
            }

            WriteEvent(
                "HudRefresh",
                $"Disposed={disposed} Skipped={skipped} HudStates={_hudStates.Count}");
        }

        private void ExecuteHudPrune()
        {
            List<int> toDispose = new();

            lock (_nameplateLock)
            {
                if (_nameplates.Count <= MaxHudCache)
                    return;

                int startCount = _nameplates.Count;

                List<KeyValuePair<int, D3DObj>> candidates = new();

                foreach (var kvp in _nameplates)
                {
                    if (kvp.Value.Visible)
                        continue;

                    candidates.Add(kvp);
                }

                candidates.Sort((a, b) =>
                    _hudStates[a.Key].HiddenOrder.CompareTo(
                        _hudStates[b.Key].HiddenOrder));

                foreach (var kvp in candidates)
                {
                    toDispose.Add(kvp.Key);

                    if (startCount - toDispose.Count <= TargetHudCache)
                        break;
                }

                if (toDispose.Count == 0)
                    return;

                WriteEvent(
                    "HudPrune",
                    $"Started={startCount}, Queued={toDispose.Count}, Target={TargetHudCache}");
            }

            lock (_renderPendingLock)
            {
                foreach (int objectId in toDispose)
                {
                    _pendingDisposes.Enqueue(objectId);
                }
            }
        }

        private void ProcessPendingRenderActions()
        {
            ExecutePendingDisposes();
            ExecutePendingCreates();
            ExecutePendingVisibility();
        }

        private void CleanupDeadHuds()
        {
            int startedWith;
            int removed = 0;

            List<KeyValuePair<int, D3DObj>> nameplates;

            lock (_nameplateLock)
            {
                startedWith = _nameplates.Count;
                nameplates = _nameplates.ToList();
            }

            var disposeIds = new List<int>();

            foreach (var pair in nameplates)
            {
                var wo =
                    CoreManager.Current.WorldFilter[pair.Key];

                lock (_nameplateLock)
                {
                    if (wo != null)
                    {
                        _pendingRemovalChecks.Remove(pair.Key);
                        continue;
                    }

                    if (!_pendingRemovalChecks.ContainsKey(pair.Key))
                    {
                        _pendingRemovalChecks[pair.Key] =
                            DateTime.UtcNow;

                        continue;
                    }

                    if (DateTime.UtcNow -
                            _pendingRemovalChecks[pair.Key] <
                        TimeSpan.FromSeconds(2))
                    {
                        continue;
                    }

                    if (_hudStates.TryGetValue(pair.Key, out var hud) &&
                        !hud.DisposeQueued)
                    {
                        hud.DisposeQueued = true;
                        disposeIds.Add(pair.Key);
                        removed++;
                    }
                }
            }

            if (disposeIds.Count > 0)
            {
                lock (_renderPendingLock)
                {
                    foreach (int id in disposeIds)
                    {
                        _pendingDisposes.Enqueue(id);
                    }
                }

                int nameplateCount;
                int hudStates;
                int pendingRemovalChecks;
    

                lock (_nameplateLock)
                {
                    nameplateCount = _nameplates.Count;
                    hudStates = _hudStates.Count;
                    pendingRemovalChecks = _pendingRemovalChecks.Count;
                }

                int pendingCreates;
                int pendingDisposes;

                lock (_renderPendingLock)
                {
                    pendingCreates = _pendingCreates.Count;
                    pendingDisposes = _pendingDisposes.Count;
                }

                int requestedIds;

                lock (_requestedObjectIdsLock)
                {
                    requestedIds = _requestedObjectIds.Count;
                }

                long memoryMb =
                    GC.GetTotalMemory(false) / 1024 / 1024;

                WriteEvent(
                    "NameplateCleanup",
                    $"Started={startedWith}, Queued={removed}");

                WriteEvent(
                    "HudStats",
                    $"Nameplates={nameplateCount}"
                    + $" HudStates={hudStates}"
                    + $" PendingCreates={pendingCreates}"
                    + $" PendingDisposes={pendingDisposes}"
                    + $" RequestedIds={requestedIds}"
                    + $" PendingRemovalChecks={pendingRemovalChecks}"
                    + $" Memory={memoryMb}MB");

            }
        }

        /*
        private void ResetAllNameplates()
        {
            lock (_nameplateLock)
            {
                Chat("ACTrigger Resetting all HUDs.");
                WriteEvent(
                    "ResetAllNameplates",
                    $"Disposing {_nameplates.Count} HUDs");

                foreach (var pair in _nameplates)
                {
                    try
                    {
                        pair.Value.Visible = false;
                    }
                    catch
                    {
                    }

                    try
                    {
                        ((DisposableByRefObject)pair.Value)
                            .Dispose();
                    }
                    catch (Exception ex)
                    {
                        WriteError(
                            nameof(ResetAllNameplates),
                            ex);
                    }
                }

                _nameplates.Clear();

                WriteEvent(
                    "ResetAllNameplates",
                    $"Remaining after clear={_nameplates.Count}");

                _pendingRemovalChecks.Clear();

                _hudStates.Clear();

                lock (_renderPendingLock)
                {
                    _pendingCreates.Clear();
                    _pendingDisposes.Clear();
                    _requestedObjectIds.Clear();
                }

                
            }
        }
        */

        private static bool ShouldUseSmallNameplate(
            WorldObject wo,
            float height)
        {
            return
                height < 1.0f &&
                wo.Name?.IndexOf(
                    "Pet",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }
        
        private void QueuePendingObject(int objectId)
        {
            lock (_pendingObjectLock)
            {
                if (_pendingObjectSet.Add(objectId))
                    _pendingObjects.Enqueue(objectId);
            }
        }

        private void ProcessPendingObjects()
        {
            while (true)
            {
                int objectId;

                lock (_pendingObjectLock)
                {
                    if (_pendingObjects.Count == 0)
                        return;

                    objectId = _pendingObjects.Dequeue();
                    _pendingObjectSet.Remove(objectId);
                }

                var wo = CoreManager.Current.WorldFilter[objectId];

                if (wo != null)
                    ProcessWorldObject(wo);
            }
        }
        
        private void ProcessWorldObject(WorldObject wo)
        {
            if (!_hudsEnabled)
                return;

            if (!ShouldProcessHud(wo))
                return;

            HudState hud = GetOrCreateHudState(wo);
            bool hasHud = TrackVisibleHud(wo);

            string name = wo.Name ?? "";
            int level = 0;
            if (wo.ObjectClass == ObjectClass.Player || wo.ObjectClass == ObjectClass.Monster)
                level = wo.Values((LongValueKey)25, 0);

            string monarch = "";
            if (wo.ObjectClass == ObjectClass.Player)
                monarch = wo.Values((StringValueKey)21, "");

            bool metadataChanged = false;

            lock (_nameplateLock)
            {
                if (string.IsNullOrEmpty(hud.DesiredName) && !string.IsNullOrEmpty(name))
                {
                    hud.DesiredName = name;
                    metadataChanged = true;
                }
                if (hud.DesiredLevel == 0 && level > 0)
                {
                    hud.DesiredLevel = level;
                    metadataChanged = true;
                }
                if (string.IsNullOrEmpty(hud.DesiredMonarch) && !string.IsNullOrEmpty(monarch))
                {
                    hud.DesiredMonarch = monarch;
                    metadataChanged = true;
                }
                if (metadataChanged)
                    MarkHudDirty(hud);
            }

            bool ready = !string.IsNullOrEmpty(hud.DesiredName);
            if (wo.ObjectClass == ObjectClass.Player || wo.ObjectClass == ObjectClass.Monster)
                ready &= hud.DesiredLevel > 0;

            if (metadataChanged && ready)
                RequestHudIfNeeded(wo, hud);

            EnsureHudExists(wo, hasHud);
        }

        private void ProcessAwaitingHuds()
        {
            if (!_hudsEnabled)
                return;

            List<int> ids;

            lock (_awaitingHudLock)
            {
                if (_awaitingHud.Count == 0)
                    return;

                ids = _awaitingHud.ToList();
            }

            foreach (int id in ids)
            {
                WorldObject wo = CoreManager.Current.WorldFilter[id];

                if (wo == null)
                    continue;

                ProcessAwaitingHud(wo);
            }
        }

        private void ProcessAwaitingHud(WorldObject wo)
        {
            if (!ShouldProcessHud(wo))
                return;

            bool hasHud = TrackVisibleHud(wo);

            EnsureHudExists(wo, hasHud);
        }

        private bool ShouldProcessHud(WorldObject wo)
        {
            if (wo == null)
                return false;

            if (!ShouldHaveHud(wo))
                return false;

            if (string.IsNullOrWhiteSpace(wo.Name))
                return false;

            return true;
        }

        private void RequestHudIfNeeded(
            WorldObject wo,
            HudState hud)
        {
            if (hud.RegenerationRequested)
                return;

            if (wo.ObjectClass != ObjectClass.Player)
            {
                string file = GetHudFile(wo);

                if (File.Exists(file))
                    return;
            }

            hud.RegenerationRequested = true;

            lock (_awaitingHudLock)
            {
                _awaitingHud.Add(wo.Id);
            }

            WriteEvent(
                "HudRequest",
                $"{hud.ObjectId}|{CoreManager.Current.CharacterFilter.Server}|"
                + $"{wo.ObjectClass}|{hud.DesiredName}|"
                + $"{hud.DesiredLevel}|{hud.DesiredMonarch}");
        }

        private bool IsGeneratedHudReady(
            string file,
            HudState hud)
        {
            if (!File.Exists(file))
                return false;

            DateTime writeTime =
                File.GetLastWriteTimeUtc(file);

            if (writeTime <= hud.LastHudWriteTimeUtc)
                return false;

            hud.LastHudWriteTimeUtc = writeTime;
            hud.CacheDirty = false;
            hud.RegenerationRequested = false;

            return true;
        }
        
        private void EnsureHudExists(WorldObject wo, bool hasHud)
        {
            HudState hud;

            lock (_nameplateLock)
            {
                if (!_hudStates.TryGetValue(wo.Id, out hud))
                    return;
            }

            if (!hasHud)
            {
                string file = GetHudFile(wo);

                if (hud.CacheDirty)
                {
                    if (!IsGeneratedHudReady(file, hud))
                    {
                        return;
                    }
                }

                if (!File.Exists(file))
                {
                    return;
                }

                if (hud.ImageWidth == 0 ||
                    hud.ImageHeight == 0)
                {
                    if (!TryGetHudDimensions(
                            file,
                            out hud.ImageWidth,
                            out hud.ImageHeight))
                    {
                        return;
                    }
                }

                bool shouldQueue = false;

                lock (_nameplateLock)
                {
                    if (!hud.CreateQueued)
                    {
                        hud.CreateQueued = true;
                        shouldQueue = true;
                    }
                }

                if (shouldQueue)
                {
                    lock (_renderPendingLock)
                    {
                        _pendingCreates.Enqueue(
                            new PendingCreate
                            {
                                ObjectId = wo.Id,
                                File = file,
                                ImageWidth = hud.ImageWidth,
                                ImageHeight = hud.ImageHeight
                            });
                    }
                }
                return;
            }
        }

        private bool TrackVisibleHud(WorldObject wo)
        {
            lock (_nameplateLock)
            {
                _pendingRemovalChecks.Remove(wo.Id);
                return _nameplates.ContainsKey(wo.Id);
            }
        }

        private void ResetRequestedObjectIds()
        {
            int count;

            lock (_requestedObjectIdsLock)
            {
                count = _requestedObjectIds.Count;
                _requestedObjectIds.Clear();
            }

            WriteEvent(
                "CacheReset",
                $"RequestedObjectIds={count}");
        }

        private void SetHudsEnabled(bool enabled)
        {
            if (_hudsEnabled == enabled)
            {
                Chat(enabled
                    ? "HUDs are already enabled."
                    : "HUDs are already disabled.");

                return;
            }

            _hudsEnabled = enabled;

            if (!enabled)
            {
                RefreshHudNameplates();
            }

            if (enabled)
            {
                Chat("HUDs enabled.");
            }
            else
            {
                Chat("HUDs disabled.");
            }
        }

        private void RefreshHudNameplates()
        {
            _refreshHudsRequested = true;
        }

        private void Current_CommandLineText(
            object sender,
            ChatParserInterceptEventArgs e)
        {
            if (!e.Text.StartsWith("/act", StringComparison.OrdinalIgnoreCase))
                return;

            e.Eat = true;

            string[] args =
                e.Text.Split(
                    new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            if (args.Length < 2)
            {
                Chat("Usage: /act <command>");
                Chat("Type /act help for available commands.");
                return;
            }

            switch (args[1].ToLowerInvariant())
            {
                case "dpson":
                    _combatTracker.Reset();
                    _dpsEnabled = true;
                    _dpsWindow.Create();
                    Chat("DPS window enabled.");
                    break;

                case "dpsoff":
                    _dpsEnabled = false;
                    _combatTracker.Reset();
                    _dpsWindow.Destroy();
                    Chat("DPS window disabled.");
                    break;

                case "timeout":
                {
                    if (args.Length == 2)
                    {
                        Chat($"Combat timeout: {_combatTimeoutSeconds} seconds.");
                        break;
                    }

                    if (int.TryParse(args[2], out int seconds) &&
                        seconds >= 1 &&
                        seconds <= 60)
                    {
                        _combatTimeoutSeconds = seconds;
                        Chat($"Combat timeout set to {seconds} seconds.");
                    }
                    else
                    {
                        Chat("Usage: /act timeout <1-60>");
                    }

                    break;
                }

                case "dpsreset":
                    _combatTracker.Reset();

                    if (_dpsEnabled)
                    {
                        _dpsWindow.SetStats(
                            0,
                            0,
                            0,
                            0,
                            "0.0s");
                    }

                    Chat("DPS reset.");
                    break;

                case "hudrefresh":
                    RefreshHudNameplates();
                    Chat("HUD refresh queued.");
                    break;

                case "huds":
                {
                    if (args.Length == 2)
                    {
                        Chat(_hudsEnabled
                            ? "HUDs are enabled."
                            : "HUDs are disabled.");

                        break;
                    }

                    switch (args[2].ToLowerInvariant())
                    {
                        case "on":
                            SetHudsEnabled(true);
                            break;

                        case "off":
                            SetHudsEnabled(false);
                            break;

                        default:
                            Chat("Usage:");
                            Chat("/act huds");
                            Chat("/act huds on.");
                            Chat("/act huds off.");
                            break;
                    }

                    break;
                }

                case "help":
                    Chat("ACTrigger Commands:");
                    Chat("/act help - Show this help.");
                    Chat("/act dpson - Show the DPS window.");
                    Chat("/act dpsoff - Hide the DPS window.");
                    Chat("/act timeout - Show combat timeout.");
                    Chat("/act timeout <1-60> - Set combat timeout.");
                    Chat("/act dpsreset - Reset DPS stats.");
                    Chat("/act hudrefresh - Rebuild all HUD nameplates.");
                    Chat("/act HUDS on - Turn on HUDs.");
                    Chat("/act HUDS off - Turn off HUDs.");
                    break;
            }
        }

        private static string FormatTime(int seconds)
        {
            TimeSpan ts = TimeSpan.FromSeconds(seconds);

            if (ts.TotalMinutes >= 1)
                return $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s";

            return $"{ts.Seconds}s";
        }
    }
    public sealed class CombatTracker
    {
        public DateTime StartTime { get; private set; }
        public DateTime LastDamageTime { get; private set; }

        public long TotalDamage { get; private set; }
        public int MaxHit { get; private set; }
        public int CriticalHits { get; private set; }

        public bool Active { get; private set; }

        public int CurrentDps { get; private set; }
        public double Duration { get; private set; }
        public int ElapsedSeconds { get; private set; }

        public void AddDamage(
            int damage,
            bool critical)
        {
            var now = DateTime.UtcNow;

            if (!Active)
            {
                Active = true;
                StartTime = now;
                ElapsedSeconds = 0;

                TotalDamage = 0;
                MaxHit = 0;
                CriticalHits = 0;
                CurrentDps = 0;
                Duration = 0;
            }

            LastDamageTime = now;
            TotalDamage += damage;

            if (damage > MaxHit)
                MaxHit = damage;

            if (critical)
                CriticalHits++;

            Duration = (now - StartTime).TotalSeconds;

            if (Duration < 1.0)
            {
                CurrentDps = (int)TotalDamage;
            }
            else
            {
                CurrentDps = (int)(TotalDamage / Duration);
            }
        }

        public void Reset()
        {
            Active = false;
            TotalDamage = 0;
            MaxHit = 0;
            CriticalHits = 0;
            CurrentDps = 0;
            Duration = 0;
            StartTime = default;
            LastDamageTime = default;
            ElapsedSeconds = 0;
        }

        public void Update(int timeoutSeconds)
        {
            if (!Active)
                return;

            if ((DateTime.UtcNow - LastDamageTime).TotalSeconds >= timeoutSeconds)
            {
                Duration = (LastDamageTime - StartTime).TotalSeconds;

                ElapsedSeconds = (int)Math.Round(Duration);

                Active = false;
            }
        }

        public void Touch()
        {
            if (Active)
                LastDamageTime = DateTime.UtcNow;
        }

        public void TickSecond()
        {
            if (Active)
                ElapsedSeconds++;
        }

    }
}
