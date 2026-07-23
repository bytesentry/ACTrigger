using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ACTrigger.UI.Rendering;

public static class NameplateGenerator
{
    private static readonly IBrush PlayerBrush =
        new SolidColorBrush(
            Color.Parse("#7FD3FF"));

    private static readonly IBrush FellowBrush =
        Brushes.Yellow;

    private static readonly IBrush AllegianceBrush =
        Brushes.Orange;

    private static readonly IBrush MonsterBrush =
        new SolidColorBrush(
            Color.Parse("#D88C2D"));

    private static readonly IBrush NpcBrush =
        new SolidColorBrush(
            Color.Parse("#72D672"));

    private static readonly IBrush PortalBrush =
        new SolidColorBrush(
            Color.Parse("#C28BFF"));

    private static readonly IBrush VendorBrush =
        Brushes.White;

    private static readonly IBrush PetBrush =
        new SolidColorBrush(
            Color.Parse("#78E4B5"));

    public static string FontName { get; set; } =
        "Palatino Linotype";
    
    
    public static async Task<string> GenerateAsync(
        string text,
        HudType type,
        HudType ownerType,
        HudStyle style,
        string server,
        string pluginPath,
        CancellationToken cancellationToken = default)
    {
        string cacheDirectory;

        if (type == ownerType)
        {
            cacheDirectory = Path.Combine(
                pluginPath,
                "Assets",
                "Cache",
                server,
                "Nameplates",
                ownerType.ToString());
        }
        else
        {
            cacheDirectory = Path.Combine(
                pluginPath,
                "Assets",
                "Cache",
                server,
                "Nameplates",
                ownerType.ToString(),
                type.ToString());
        }

        Directory.CreateDirectory(
            cacheDirectory);

        string safeName = MakeSafeFilename(text);

        string outputFile =
            Path.Combine(
                cacheDirectory,
                safeName + ".png");

        string renderText =
            type switch
            {
                HudType.Tag =>
                    $"< {text} >",

                HudType.Level =>
                    $"({text})",

                _ =>
                    text
            };

        var typeface =
            new Typeface(
                new FontFamily(FontName),
                FontStyle.Normal,
                FontWeight.Bold);

        // Font size varies by HUD element type.
        double fontSize =
            type == HudType.Tag
                ? 34
                : 48;

        Size textSize =
            NameplateTextRenderer.Measure(
                renderText,
                typeface,
                fontSize);

        const int padding = 2;

        int width =
            (int)Math.Ceiling(textSize.Width) +
            (padding * 2);

        const int strokeThickness = 4;

        const int targetHeight = 53;

        int height = targetHeight;

        //int height =
        //    (int)Math.Ceiling(textSize.Height) +
        //    (padding * 2) +
        //    strokeThickness -
        //     4;//account for strange bottom margin

        using var bitmap =
            new RenderTargetBitmap(
                new PixelSize(width, height));

        using (var context = bitmap.CreateDrawingContext())
        {
            NameplateTextRenderer.Draw(
                context,
                renderText,
                typeface,
                fontSize,
                GetTextBrush(type, ownerType),
                Brushes.Black,
                strokeThickness,
                new Point(padding, padding));
        }

        string tempFile =
            outputFile + ".tmp";

        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }

        using (var stream = File.Create(tempFile))
        {
            bitmap.Save(stream);
        }

        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }

        File.Move(
            tempFile,
            outputFile);

        return outputFile;
    }
    
    private static string MakeSafeFilename(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }

        return name;
    }

    private static IBrush GetTextBrush(
        HudType type,
        HudType ownerType)
    {
        return type switch
        {
            // Name assets use their own type.
            HudType.Player => PlayerBrush,
            HudType.Monster => MonsterBrush,
            HudType.Pet => PetBrush,
            HudType.Npc => NpcBrush,
            HudType.Vendor => VendorBrush,
            HudType.Portal => PortalBrush,

            // Context-sensitive assets inherit their owner's color.
            HudType.Level => ownerType switch
            {
                HudType.Monster => MonsterBrush,
                HudType.Player => PlayerBrush,
                HudType.Pet => PetBrush,
                HudType.Npc => NpcBrush,
                HudType.Vendor => VendorBrush,
                HudType.Portal => PortalBrush,
                _ => Brushes.White
            },

            HudType.Tag => ownerType switch
            {
                HudType.Player => PlayerBrush,
                HudType.Pet => PetBrush,
                _ => PlayerBrush
            },

            _ => Brushes.White
        };
    }
}    
