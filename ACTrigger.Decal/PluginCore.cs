using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.IO;
using ACTrigger.Decal;
using System.Reflection;
using System.Timers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;


namespace ACTrigger.Decal
{
    public class Plugin : PluginBase
    {
        private FileService? _fileService;
        private System.Timers.Timer? _targetTimer;
        private string? pluginFolder;
        private string? logFile;

        //test
        //private System.Timers.Timer? _scanTimer;
        /////test//////////test//////////test//////////test/////
        /// /////test//////////test//////////test//////////test/////
        /// /////test//////////test//////////test//////////test/////
        /*
        private System.Timers.Timer? _d3dTestTimer;
        private D3DObj? _marker;

        private readonly int[] _testOptions = { 0, 1, 2, 4, 8, 16, 32, 64, 128, 256 };
        private int _testOptionIndex;
        */
        /////test//////////test//////////test//////////test/////
        /// /////test//////////test//////////test//////////test/////
        /// /////test//////////test//////////test//////////test/////

        protected override void Startup()
        {
            pluginFolder = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().Location);

            logFile = System.IO.Path.Combine(pluginFolder, "actrigger.log");

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
                WriteEvent(
                    "System",
                    $"ERROR:{ex}");
            }
            

            CoreManager.Current.WorldFilter.ReleaseObject +=
                WorldFilter_ReleaseObject;
            
            CoreManager.Current.WorldFilter.ChangeObject +=
                WorldFilter_ChangeObject;
            
            CoreManager.Current.CharacterFilter.SpellCast +=
                CharacterFilter_SpellCast;

            CoreManager.Current.CharacterFilter.ChangePortalMode +=
                CharacterFilter_ChangePortalMode;

            
            CoreManager.Current.CharacterFilter.ChangeEnchantments +=
                CharacterFilter_ChangeEnchantments;



            CoreManager.Current.ChatBoxMessage +=
                Current_ChatBoxMessage;


            WriteEvent(
                "System",
                $"CoreType:{CoreManager.Current.GetType().FullName}");


            // check for target provided by file
            
            _targetTimer =
                new System.Timers.Timer(250);

            _targetTimer.Elapsed +=
                (_, _) => CheckTargetRequest();

            _targetTimer.Start();

            WriteEvent("System", "ACTrigger started");
            Chat("ACTrigger is active.");
        
            //////////////////////////////
            /// startup test variables
            /*
            _d3dTestTimer = new System.Timers.Timer(3000);
            _d3dTestTimer.AutoReset = true;
            _d3dTestTimer.Elapsed += D3dTest;
            _d3dTestTimer.Start();
            */
            //////////////////////////////////
        }

        ///////////////////////////test area//////////////////////
        /// ///////////////////////////test area//////////////////////
        /// ///////////////////////////test area//////////////////////
        /*
        private void D3dTest(object? sender, ElapsedEventArgs e)
        {
            try
            {
                var targetId = CoreManager.Current.Actions.CurrentSelection;

                if (targetId == 0) { WriteEvent("System", "NO TARGET"); return; }

                if (_marker == null)
                {
                    _marker = CoreManager.Current.D3DService.NewD3DObj();
                    _marker.SetText(D3DTextType.Text3D, "Mob Name Here", "", 1);
                    _marker.Color = unchecked((int)0xFFFFFF00);
                    _marker.Anchor(targetId, 2.0f, 0, 0, 0);
                    _marker.Autoscale = true;
                    _marker.Scale(0.5f);
                    _marker.Visible = true;
                    Chat("Marker created");
                }
            }
            catch (Exception ex)
            {
                WriteEvent("System", $"D3D TIMER ERROR: {ex}");
                _d3dTestTimer?.Stop();
            }
        } */
        ///////////////////////////end test area///////////////////////
        /// ///////////////////////////end test area///////////////////////
        /// ///////////////////////////end test area///////////////////////
        
        private void Chat(
            string message)
        {
            CoreManager.Current.Actions.AddChatText(
                $"<{{ACTrigger}}>: {message}",
                5);
        }

        protected override void Shutdown()
        {
            try
            {                                
                
                _targetTimer?.Stop();
                _targetTimer?.Dispose();
                
                CoreManager.Current.WorldFilter.ReleaseObject -=
                    WorldFilter_ReleaseObject;
                
                CoreManager.Current.WorldFilter.ChangeObject -=
                    WorldFilter_ChangeObject;
                
                CoreManager.Current.CharacterFilter.ChangeEnchantments -=
                    CharacterFilter_ChangeEnchantments;


                CoreManager.Current.ChatBoxMessage -=
                    Current_ChatBoxMessage;

                CoreManager.Current.CharacterFilter.SpellCast -=
                    CharacterFilter_SpellCast;
                CoreManager.Current.CharacterFilter.ChangePortalMode -=
                    CharacterFilter_ChangePortalMode;

            }
            catch (Exception ex)
            {
                WriteEvent(
                    "System",
                    $"ShutdownError:{ex.Message}");
            }

            WriteEvent(
                "System",
                "ACTrigger stopped");

            Thread.Sleep(1000);

            File.WriteAllText(logFile, string.Empty);
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
                WriteEvent(
                    "System",
                    $"ERROR:{ex.Message}");
            }
        }

        private void CharacterFilter_ChangeVital(
            object sender,
            ChangeVitalEventArgs e)
        {
            
        }

        private void Current_ChatBoxMessage(
            object sender,
            ChatTextInterceptEventArgs e)
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

            WriteEvent(
                channel,
                text);
        }

        private string ClassifyChat(string text)
        {
            // Combat gets first priority.
            if (CombatClassifier.IsCombat(text))
                return "Combat";

            // Spell incantations.
            if (ChatClassifier.IsSpellCastingMessage(text))
                return "SpellCast";

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

                case ChatClassifier.ChatChannels.Tells:
                    return "Tell";
            }

            // Non-chat messages that still come through ChatBoxMessage.
            if (text.Contains("obtain "))
                return "Loot";

            if (text.Contains("using your knowledge of"))
                return "Loot";

            if (text.Contains("learn the "))
                return "System";

            if (text.Contains("You are now level"))
                return "System";

            // Anything we don't recognize is still chat.
            return "Chat";
        }

        private void WriteEvent(
            string channel,
            string message)
        {
            message = message.Replace(
                Environment.NewLine,
                " ");

            File.AppendAllText(
                logFile,
                $"{DateTime.UtcNow:O}|{channel}|{message}\r\n");
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
                }
                WriteEvent(
                    "WorldObject",
                    $"Name={wo.Name}" +
                    $" | Id={wo.Id}" +
                    $" | Class={wo.ObjectClass}");
            }
            catch (Exception ex)
            {
                WriteEvent(
                    "SpellCastError",
                    ex.ToString());
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

                if (wo.ObjectClass != ObjectClass.Monster &&
                    wo.ObjectClass != ObjectClass.Corpse)
                {
                    return;
                }

                _ = VerifyReleasedObject(wo.Id);

            }
            catch (Exception ex)
            {
                WriteEvent(
                    "ReleaseObjectError",
                    ex.ToString());
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
        
        private void WorldFilter_ChangeObject(
            object sender,
            ChangeObjectEventArgs e)
        {
            try
            {
                var wo = e.Changed;

                if (wo == null)
                    return;

                if (wo.ObjectClass != ObjectClass.Monster)
                    return;

                WriteEvent(
                    "ObjectChange",
                    $"Id={wo.Id} Name={wo.Name}");
            }
            catch
            {
            }
        }
        /* before we test detecting other people's buffs
        private void WorldFilter_ChangeObject(
            object sender,
            ChangeObjectEventArgs e)
        {
            try
            {
                var wo = e.Changed;

                if (wo == null)
                    return;

                if (wo.ObjectClass != ObjectClass.Monster)
                    return;

                WriteEvent(
                    "ObjectChange",
                    $"Id={wo.Id} Name={wo.Name}");
            }
            catch
            {
            }
        }*/

        private string GetTargetRequestPath()
        {
            return System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(
                    Assembly.GetExecutingAssembly().Location),
                "actrigger.target");
        }

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
                WriteEvent(
                    "TargetRequestError",
                    ex.ToString());
            }
        }

        //verify object is actually dead or gone before releasing
        private async Task VerifyReleasedObject(int id)
        {
            await Task.Delay(1000);

            var lookup =
                CoreManager.Current.WorldFilter[id];

            if (lookup == null)
            {
                WriteEvent(
                    "ReleaseVerified",
                    $"Id={id}");
            }
        }

        private void CharacterFilter_ChangePortalMode(
            object sender,
            ChangePortalModeEventArgs e)
        {
            if (e.Type == PortalEventType.ExitPortal)
            {
                WriteEvent("PortalComplete", "");
            }
        }

    }
}