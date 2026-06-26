using System.Media;

namespace ACTrigger.Core.Services;

public class SoundService
{
    public void Play(string soundPath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var player = new SoundPlayer(soundPath);
                player.Play();
            }
            else
            {
                System.Diagnostics.Process.Start(
                    "pw-play",
                    soundPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Sound error: {ex}");
        }
    }
}