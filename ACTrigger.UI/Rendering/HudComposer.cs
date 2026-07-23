using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ACTrigger.UI.Rendering;

public static class HudComposer
{
    private const int Padding = 0;

    private const int HorizontalSpacing = 2;

    private const int VerticalSpacing = 0;

    private sealed class RowInfo
    {
        public required List<HudElement> Elements
        {
            get;
            init;
        }

        public int Width
        {
            get;
            init;
        }

        public int Height
        {
            get;
            init;
        }
    }

    public static (int Width, int Height) Compose(
        IEnumerable<HudElement> elements,
        string outputFile)
    {
        var rows =
            elements
                .GroupBy(
                    x => x.Row)
                .OrderBy(
                    x => x.Key)
                .Select(
                    x => x
                        .OrderBy(
                            y => y.Order)
                        .ToList())
                .ToList();

        var measuredRows =
            rows
                .Select(
                    row =>
                    {
                        int width = 0;

                        int height = 0;

                        bool first = true;

                        foreach (var element in row)
                        {
                            if (!first)
                            {
                                width += HorizontalSpacing;
                            }

                            using var bitmap =
                                new Bitmap(
                                    element.FileName);

                            width +=
                                bitmap.PixelSize.Width;

                            height =
                                Math.Max(
                                    height,
                                    bitmap.PixelSize.Height);

                            first = false;
                        }
                        return new RowInfo
                        {
                            Elements = row,
                            Width = width,
                            Height = height
                        };
                    })
                .ToList();

        int canvasWidth =
            measuredRows.Max(x => x.Width) +
            (Padding * 2);

        int canvasHeight =
            measuredRows.Sum(x => x.Height) +
            VerticalSpacing *
                Math.Max(
                    0,
                    measuredRows.Count - 1) +
            (Padding * 2);

        using var bitmap =
            new RenderTargetBitmap(
                new PixelSize(
                    canvasWidth,
                    canvasHeight));

        using (var context =
            bitmap.CreateDrawingContext())
        {
            // Draw rows from top to bottom.
            int y = Padding;

            foreach (var row in measuredRows)
            {
                int x =
                    (canvasWidth - row.Width) / 2;

                foreach (var element in row.Elements)
                {
                    using var image =
                        new Bitmap(
                            element.FileName);

                    context.DrawImage(
                        image,
                        new Rect(
                            0,
                            0,
                            image.PixelSize.Width,
                            image.PixelSize.Height),
                        new Rect(
                            x,
                            y,
                            image.PixelSize.Width,
                            image.PixelSize.Height));

                    x +=
                        image.PixelSize.Width +
                        HorizontalSpacing;
                }

                y +=
                    row.Height +
                    VerticalSpacing;
            }
        }

        string? directory =
            Path.GetDirectoryName(
                outputFile);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        string tempFile =
            outputFile + ".tmp";

        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }

        using (var stream =
            File.Create(tempFile))
        {
            bitmap.Save(stream);
        }

        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }

        File.Move(tempFile, outputFile);

        Console.WriteLine($"COMPOSE DONE {outputFile}");
            
        return (canvasWidth, canvasHeight);
    }
}