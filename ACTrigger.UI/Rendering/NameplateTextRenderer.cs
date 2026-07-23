using System.Globalization;
using Avalonia;
using Avalonia.Media;
using System;

namespace ACTrigger.UI.Rendering;

public static class NameplateTextRenderer
{
    public static Size Measure(
        string text,
        Typeface typeface,
        double fontSize)
    {
        var formatted = new FormattedText(
            text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.White);

        var geometry = formatted.BuildGeometry(new Point(0, 0));

        if (geometry is null)
            return default;

        var bounds = geometry.Bounds;

        return new Size(
            bounds.Width,
            bounds.Height);
    }

    public static void Draw(
        DrawingContext context,
        string text,
        Typeface typeface,
        double fontSize,
        IBrush fill,
        IBrush stroke,
        double strokeThickness,
        Point position)
    {
        var formatted =
            new FormattedText(
                text,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.White);

       var geometry =
            formatted.BuildGeometry(new Point(0, 0));

        if (geometry is null)
            return;

        var bounds = geometry.Bounds;

        var translated =
            formatted.BuildGeometry(
                new Point(
                    position.X - bounds.Left,
                    position.Y - bounds.Top));

        if (translated is null)
            return;

        // Draw the outline only.
        context.DrawGeometry(
            null,
            new Pen(
                stroke,
                strokeThickness),
            translated);

        // Draw the fill on top.
        context.DrawGeometry(
            fill,
            null,
            translated);
    }
}