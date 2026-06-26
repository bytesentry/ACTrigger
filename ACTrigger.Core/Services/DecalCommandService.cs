using System.IO;

namespace ACTrigger.Core.Services;

public static class DecalCommandService
{
    private const string CommandFile =
        "actrigger_commands.txt";

    public static void TargetMonster(
        int monsterId)
    {
        File.AppendAllText(
            CommandFile,
            $"TARGET|{monsterId}{System.Environment.NewLine}");
    }
}