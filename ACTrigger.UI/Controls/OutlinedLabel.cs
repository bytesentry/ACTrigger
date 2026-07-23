using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ACTrigger.UI.Controls;

public class OutlinedLabel : Control
{
    static OutlinedLabel()
    {
        AffectsRender<OutlinedLabel>(
            TextProperty,
            FillProperty,
            OutlineProperty,
            ThicknessProperty,
            FontSizeProperty);

        AffectsMeasure<OutlinedLabel>(
            TextProperty,
            ThicknessProperty,
            FontSizeProperty);
    }
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<OutlinedLabel, string?>(nameof(Text), "");

    public static readonly StyledProperty<IBrush> FillProperty =
        AvaloniaProperty.Register<OutlinedLabel, IBrush>(
            nameof(Fill),
            Brushes.White);

    public static readonly StyledProperty<IBrush> OutlineProperty =
        AvaloniaProperty.Register<OutlinedLabel, IBrush>(
            nameof(Outline),
            Brushes.Black);

    public static readonly StyledProperty<int> ThicknessProperty =
        AvaloniaProperty.Register<OutlinedLabel, int>(
            nameof(Thickness),
            1);

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<OutlinedLabel, double>(
            nameof(FontSize),
            18);

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<OutlinedLabel, FontFamily>(
            nameof(FontFamily),
            new FontFamily("Times New Roman"));

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IBrush Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush Outline
    {
        get => GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    public int Thickness
    {
        get => GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var typeface = new Typeface(
            FontFamily,
            FontStyle.Normal,
            FontWeight.Bold);

        TextRenderer.DrawOutlinedText(
            context,
            Text ?? "",
            typeface,
            FontSize,
            Fill,
            Outline,
            new Point(Thickness, Thickness),
            Thickness);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var formatted = new FormattedText(
            Text ?? "",
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                FontFamily,
                FontStyle.Normal,
                FontWeight.Bold),
            FontSize,
            Fill);

        return new Size(
            formatted.Width + (Thickness * 2),
            formatted.Height + (Thickness * 2));
    }
    
}