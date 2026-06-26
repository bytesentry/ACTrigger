using System;
using System.Text.RegularExpressions;

namespace ACTrigger.Decal;

public static class ChatClassifier
{

    private static readonly Regex PlayerSaysLocal = 
        new Regex("^<Tell:IIDString:[0-9]+:(?<name>[\\w\\s'-]+)>[\\w\\s'-]+<\\\\Tell> says, \"(?<msg>.*)\"$");
    private static readonly Regex PlayerSaysChannel = 
        new Regex("^\\[(?<channel>.+)]+ <Tell:IIDString:[0-9]+:(?<name>[\\w\\s'-]+)>[\\w\\s'-]+<\\\\Tell> says, \"(?<msg>.*)\"$");
    private static readonly Regex YouSay = 
        new Regex("^You say, \"(?<msg>.*)\"$");
    private static readonly Regex YouSaySpellCast = 
        new Regex("^You say, \"(Zojak|Malar|Puish|Cruath|Volae|Quavosh|Shurov|Boquar|Helkas|Equin|Roiga|Malar|Jevak|Tugak|Slavu|Drostu|Traku|Yanoi|Drosta|Feazh) .*\"$");
    private static readonly Regex PlayerSaysSpellCast = 
        new Regex("^<Tell:IIDString:[0-9]+:(?<name>[\\w\\s'-]+)>[\\w\\s'-]+<\\\\Tell> says, \"(Zojak|Malar|Puish|Cruath|Volae|Quavosh|Shurov|Boquar|Helkas|Equin|Roiga|Malar|Jevak|Tugak|Slavu|Drostu|Traku|Yanoi|Drosta|Feazh) .*\"$");
    private static readonly Regex PlayerTellsYou = 
        new Regex("^<Tell:IIDString:[0-9]+:(?<name>[\\w\\s'-]+)>[\\w\\s'-]+<\\\\Tell> tells you, \"(?<msg>.*)\"$");
    private static readonly Regex YouTell = 
        new Regex("^You tell .+, \"(?<msg>.*)\"$");
    private static readonly Regex NpcSays = 
        new Regex("^(?<name>[\\w\\s'-]+) says, \"(?<msg>.*)\"$");
    private static readonly Regex NpcTellsYou = 
        new Regex("^(?<name>[\\w\\s'-]+) tells you, \"(?<msg>.*)\"$");

    [Flags]
    public enum ChatFlags : byte
    {
        None				= 0x00,

        PlayerSaysLocal		= 0x01,
        PlayerSaysChannel	= 0x02,
        YouSay				= 0x04,

        PlayerTellsYou		= 0x08,
        YouTell				= 0x10,

        NpcSays				= 0x20,
        NpcTellsYou			= 0x40,

        All					= 0xFF,
    }
    [Flags]
    public enum ChatChannels : ushort
    {
        None		= 0x0000,

        Area		= 0x0001,
        Tells		= 0x0002,

        Fellowship	= 0x0004,
        Allegiance	= 0x0008,
        General		= 0x0010,
        Trade		= 0x0020,
        LFG			= 0x0040,
        Roleplay	= 0x0080,
        Society		= 0x0100,

        All			= 0xFFFF,
    }
    public static bool IsChat(string text, ChatFlags chatFlags = ChatFlags.All)
    {
        if ((chatFlags & ChatFlags.PlayerSaysLocal) == ChatFlags.PlayerSaysLocal && PlayerSaysLocal.IsMatch(text))
            return true;

        if ((chatFlags & ChatFlags.PlayerSaysChannel) == ChatFlags.PlayerSaysChannel && PlayerSaysChannel.IsMatch(text))
            return true;

        if ((chatFlags & ChatFlags.YouSay) == ChatFlags.YouSay && YouSay.IsMatch(text))
            return true;


        if ((chatFlags & ChatFlags.PlayerTellsYou) == ChatFlags.PlayerTellsYou && PlayerTellsYou.IsMatch(text))
            return true;

        if ((chatFlags & ChatFlags.YouTell) == ChatFlags.YouTell && YouTell.IsMatch(text))
            return true;


        if ((chatFlags & ChatFlags.NpcSays) == ChatFlags.NpcSays && NpcSays.IsMatch(text))
            return true;

        if ((chatFlags & ChatFlags.NpcTellsYou) == ChatFlags.NpcTellsYou && NpcTellsYou.IsMatch(text))
            return true;

        return false;
    }

    public static ChatChannels GetChatChannel(string text)
    {
        if (IsChat(text, ChatFlags.PlayerSaysLocal | ChatFlags.YouSay | ChatFlags.NpcSays))
            return ChatChannels.Area;

        if (IsChat(text, ChatFlags.PlayerTellsYou | ChatFlags.YouTell | ChatFlags.NpcTellsYou))
            return ChatChannels.Tells;

        if (IsChat(text, ChatFlags.PlayerSaysChannel))
        {
            Match match = PlayerSaysChannel.Match(text);

            if (match.Success)
            {
                string channel = match.Groups["channel"].Value;

                if (channel == "Fellowship") return ChatChannels.Fellowship;
                if (channel == "Allegiance") return ChatChannels.Allegiance;
                if (channel == "General") return ChatChannels.General;
                if (channel == "Trade") return ChatChannels.Trade;
                if (channel == "LFG") return ChatChannels.LFG;
                if (channel == "Roleplay") return ChatChannels.Roleplay;
                if (channel == "Society") return ChatChannels.Society;
            }
        }

        return ChatChannels.None;
    }
    
    public static bool IsSpellCastingMessage(string text, bool isMine = true, bool isPlayer = true)
    {
        if (isMine && YouSaySpellCast.IsMatch(text))
            return true;

        if (isPlayer && PlayerSaysSpellCast.IsMatch(text))
            return true;

        return false;
    }
    
    public static string GetLogChannel(string text)
    {

        ChatChannels channel =
            GetChatChannel(text);

        return channel switch
        {
            ChatChannels.Area => "Area",
            ChatChannels.Tells => "Tell",
            ChatChannels.Fellowship => "Fellowship",
            ChatChannels.Allegiance => "Allegiance",
            ChatChannels.General => "General",
            ChatChannels.Trade => "Trade",
            ChatChannels.LFG => "LFG",
            ChatChannels.Roleplay => "Roleplay",
            ChatChannels.Society => "Society",
            _ => "Chat"
        };
    }
}