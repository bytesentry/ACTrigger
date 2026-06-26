using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using ACTrigger.Models;
using System.Collections.ObjectModel;
using System;
using ACTrigger.UI.ViewModels;

namespace ACTrigger.UI.Controls;

public class CombatTextCanvas
    : Control
{
    public static readonly StyledProperty<
        ObservableCollection<CombatText>?>
        CombatTextsProperty =
            AvaloniaProperty.Register<
                CombatTextCanvas,
                ObservableCollection<CombatText>?>(
                    nameof(CombatTexts));

    public ObservableCollection<CombatText>?
        CombatTexts
    {
        get => GetValue(
            CombatTextsProperty);

        set => SetValue(
            CombatTextsProperty,
            value);
    }
    
    private static readonly Bitmap KillBitmap =
    new Bitmap(
        AssetLoader.Open(
            new Uri(
                "avares://ACTrigger.UI/Assets/Images/skull.png")));


    public CombatTextCanvas()
    {
        //_skullBitmap = new Avalonia.Media.Imaging.Bitmap(
        //    AssetLoader.Open(new Uri("avares://ACTrigger.UI/Assets/Images/skull.png")));
            
        var timer =
            new Avalonia.Threading.DispatcherTimer
            {
                Interval =
                    TimeSpan.FromMilliseconds(16)
            };

        timer.Tick += (_, _) =>
        {
            InvalidateVisual();
        };

        timer.Start();
        
    }

    public override void Render(
        DrawingContext context)
    {
        base.Render(
            context);

        if (DataContext is not MainWindowViewModel vm)
            return;

        ObservableCollection<CombatText> texts;

        if (Name == "IncomingCanvas")
        {
            texts =
                vm.IncomingCombatTexts;
        }
        else if (Name == "KillCanvas")
        {
            texts =
                vm.KillCombatTexts;
        }
        else
        {
            texts =
                vm.OutgoingCombatTexts;
        }
        
        foreach (var text in texts)
        {
            
            if (text.Type == CombatTextType.Kill)
            {
                double size =
                    text.FontSize * text.Scale;

                using (context.PushOpacity(text.Opacity))
                {
                    context.DrawImage(
                        KillBitmap,
                        new Rect(
                            0,
                            0,
                            KillBitmap.Size.Width,
                            KillBitmap.Size.Height),
                        new Rect(
                            text.X - (size / 2),
                            text.Y,
                            size,
                            size));
                }

                continue;
            }
            
            var color =
                Color.Parse(
                    text.Color);

            var brush =
                new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(255 * text.Opacity),
                        color.R,
                        color.G,
                        color.B));

            var formatted =
                new FormattedText(
                    text.Text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(
                        new FontFamily(
                            "avares://ACTrigger.UI/Assets/Fonts#Viking-Normal"),
                        FontStyle.Normal,
                        FontWeight.Bold),
                    text.FontSize,
                    brush);

            using var transform =
                context.PushTransform(
                    Matrix.CreateTranslation(
                        -text.X,
                        -text.Y)
                    *
                    Matrix.CreateScale(
                        text.Scale,
                        text.Scale)
                    *
                    Matrix.CreateTranslation(
                        text.X,
                        text.Y));
            
            var outlineBrush =
                new SolidColorBrush(
                    Color.FromArgb(
                        (byte)(255 * text.Opacity),
                        0,
                        0,
                        0));

            var outlineText =
                new FormattedText(
                    text.Text,
                    System.Globalization.CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(
                        new FontFamily(
                            "avares://ACTrigger.UI/Assets/Fonts#Viking-Normal"),
                        FontStyle.Normal,
                        FontWeight.Bold),
                    text.FontSize,
                    outlineBrush);

            
            double drawX =
                text.X - (formatted.Width / 2);

            double drawY =
                text.Y;

            TextRenderer.DrawOutlinedText(
                context,
                text.Text,
                new Typeface(
                    new FontFamily("avares://ACTrigger.UI/Assets/Fonts#Viking-Normal"),
                    FontStyle.Normal,
                    FontWeight.Bold),
                text.FontSize,
                brush,
                outlineBrush,
                new Point(drawX, drawY),
                3);
        }
    }
}
public static class TextRenderer
{
    public static void DrawOutlinedText(
        DrawingContext context,
        string text,
        Typeface typeface,
        double fontSize,
        IBrush fill,
        IBrush outline,
        Point position,
        int thickness = 1)
    {
        var outlineText =
            new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                outline);

        var fillText =
            new FormattedText(
                text,
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                fill);

        for (int y = -thickness; y <= thickness; y++)
        {
            for (int x = -thickness; x <= thickness; x++)
            {
                if (x == 0 && y == 0)
                    continue;

                context.DrawText(
                    outlineText,
                    new Point(position.X + x, position.Y + y));
            }
        }

        context.DrawText(fillText, position);
    }
    
}
