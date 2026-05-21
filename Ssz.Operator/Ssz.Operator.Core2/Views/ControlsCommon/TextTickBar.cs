using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsCommon;

public class TextTickBar : Control
{
    #region public functions Ч свойства из TickBar (AddOwner)

    public static readonly StyledProperty<double> MinimumProperty =
        TickBar.MinimumProperty.AddOwner<TextTickBar>();

    public static readonly StyledProperty<double> MaximumProperty =
        TickBar.MaximumProperty.AddOwner<TextTickBar>();

    public static readonly StyledProperty<double> TickFrequencyProperty =
        TickBar.TickFrequencyProperty.AddOwner<TextTickBar>();

    public static readonly StyledProperty<TickBarPlacement> PlacementProperty =
        TickBar.PlacementProperty.AddOwner<TextTickBar>();

    public static readonly StyledProperty<IBrush?> FillProperty =
        TickBar.FillProperty.AddOwner<TextTickBar>();

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public TickBarPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    #endregion

    #region public functions Ч собственные свойства

    static TextTickBar()
    {
        AffectsRender<TextTickBar>(
            // —войства, унаследованные от TickBar через AddOwner
            MinimumProperty,
            MaximumProperty,
            TickFrequencyProperty,
            PlacementProperty,
            FillProperty,
            // —обственные свойства
            FontFamilyProperty,
            FontStyleProperty,
            FontWeightProperty,
            FontStretchProperty,
            FontSizeProperty,
            FormatProperty,
            EngUnitProperty,
            TextTickFrequencyProperty,
            TickStrokeThicknessProperty,
            TickLengthProperty);
    }

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<TextTickBar, FontFamily>(
            nameof(FontFamily),
            defaultValue: new FontFamily("Courier New"));

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        AvaloniaProperty.Register<TextTickBar, FontStyle>(
            nameof(FontStyle),
            defaultValue: FontStyle.Normal);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<TextTickBar, FontWeight>(
            nameof(FontWeight),
            defaultValue: FontWeight.Normal);

    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        AvaloniaProperty.Register<TextTickBar, FontStretch>(
            nameof(FontStretch),
            defaultValue: FontStretch.Normal);

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TextTickBar, double>(
            nameof(FontSize),
            defaultValue: 14.0);

    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<TextTickBar, string>(
            nameof(Format),
            defaultValue: "F02");

    public static readonly StyledProperty<string> EngUnitProperty =
        AvaloniaProperty.Register<TextTickBar, string>(
            nameof(EngUnit),
            defaultValue: "");

    public static readonly StyledProperty<double> TextTickFrequencyProperty =
        AvaloniaProperty.Register<TextTickBar, double>(
            nameof(TextTickFrequency),
            defaultValue: 0.2);

    public static readonly StyledProperty<double> TickStrokeThicknessProperty =
        AvaloniaProperty.Register<TextTickBar, double>(
            nameof(TickStrokeThickness),
            defaultValue: 1.0);

    public static readonly StyledProperty<double> TickLengthProperty =
        AvaloniaProperty.Register<TextTickBar, double>(
            nameof(TickLength),
            defaultValue: 10.0);

    public FontFamily FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public FontStyle FontStyle
    {
        get => GetValue(FontStyleProperty);
        set => SetValue(FontStyleProperty, value);
    }

    public FontWeight FontWeight
    {
        get => GetValue(FontWeightProperty);
        set => SetValue(FontWeightProperty, value);
    }

    public FontStretch FontStretch
    {
        get => GetValue(FontStretchProperty);
        set => SetValue(FontStretchProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public string Format
    {
        get => GetValue(FormatProperty);
        set => SetValue(FormatProperty, value);
    }

    public string EngUnit
    {
        get => GetValue(EngUnitProperty);
        set => SetValue(EngUnitProperty, value);
    }

    public double TextTickFrequency
    {
        get => GetValue(TextTickFrequencyProperty);
        set => SetValue(TextTickFrequencyProperty, value);
    }

    public double TickStrokeThickness
    {
        get => GetValue(TickStrokeThicknessProperty);
        set => SetValue(TickStrokeThicknessProperty, value);
    }

    public double TickLength
    {
        get => GetValue(TickLengthProperty);
        set => SetValue(TickLengthProperty, value);
    }

    public string GetValueWithEngUnit(double value)
    {
        return ObsoleteAnyHelper.ConvertTo<string>(value, true, Format) + @" " + EngUnit;
    }

    #endregion

    #region protected functions

    // Avalonia: Control.Render не sealed, поэтому override работает.
    public override void Render(DrawingContext dc)
    {
        var delta = Maximum - Minimum;

        if (delta < double.Epsilon * 1000) return;

        if (Placement == TickBarPlacement.Left)
        {
            var textTickDelta = Bounds.Height * TextTickFrequency / delta;
            if (textTickDelta > 1)
            {
                var v = Minimum;
                for (var y = Bounds.Height; y > -1; y -= textTickDelta)
                {
                    DrawTick(dc, -TickLength, y, 0, y);

                    FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                    dc.DrawText(formattedText,
                        new Point(-formattedText.Width - 1.5 * TickLength, y - formattedText.Height / 2));

                    v += TextTickFrequency;
                }
            }

            var tickDelta = Bounds.Height * TickFrequency / delta;
            if (tickDelta > 1)
                for (var y = Bounds.Height; y > -1; y -= tickDelta)
                    DrawTick(dc, -TickLength / 2, y, 0, y);

            return;
        }

        if (Placement == TickBarPlacement.Right)
        {
            var textTickDelta = Bounds.Height * TextTickFrequency / delta;
            if (textTickDelta > 1)
            {
                var v = Minimum;
                for (var y = Bounds.Height; y > -1; y -= textTickDelta)
                {
                    DrawTick(dc, 0, y, TickLength, y);

                    FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                    dc.DrawText(formattedText, new Point(1.5 * TickLength, y - formattedText.Height / 2));

                    v += TextTickFrequency;
                }
            }

            var tickDelta = Bounds.Height * TickFrequency / delta;
            if (tickDelta > 1)
                for (var y = Bounds.Height; y > -1; y -= tickDelta)
                    DrawTick(dc, 0, y, TickLength / 2, y);

            return;
        }

        if (Placement == TickBarPlacement.Top)
        {
            var textTickDelta = Bounds.Width * TextTickFrequency / delta;
            if (textTickDelta > 1)
            {
                var v = Minimum;
                for (double x = 0; x < Bounds.Width + 1; x += textTickDelta)
                {
                    DrawTick(dc, x, -TickLength, x, 0);

                    FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                    dc.DrawText(formattedText,
                        new Point(x - formattedText.Width / 2, -formattedText.Height - 1.5 * TickLength));

                    v += TextTickFrequency;
                }
            }

            var tickDelta = Bounds.Width * TickFrequency / delta;
            if (tickDelta > 1)
                for (double x = 0; x < Bounds.Width + 1; x += tickDelta)
                    DrawTick(dc, x, -TickLength / 2, x, 0);

            return;
        }

        if (Placement == TickBarPlacement.Bottom)
        {
            var textTickDelta = Bounds.Width * TextTickFrequency / delta;
            if (textTickDelta > 1)
            {
                var v = Minimum;
                for (double x = 0; x < Bounds.Width + 1; x += textTickDelta)
                {
                    DrawTick(dc, x, 0, x, TickLength);

                    FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                    dc.DrawText(formattedText, new Point(x - formattedText.Width / 2, 1.5 * TickLength));

                    v += TextTickFrequency;
                }
            }

            var tickDelta = Bounds.Width * TickFrequency / delta;
            if (tickDelta > 1)
                for (double x = 0; x < Bounds.Width + 1; x += tickDelta)
                    DrawTick(dc, x, 0, x, TickLength / 2);

            return;
        }

        base.Render(dc);
    }

    #endregion

    #region private functions

    private void DrawTick(DrawingContext dc, double x1, double y1, double x2, double y2)
    {
        var tickStrokeThickness = TickStrokeThickness;
        dc.DrawLine(new Pen(Fill, tickStrokeThickness),
            new Point(x1, y1), new Point(x2, y2));
    }

    private FormattedText GetFormattedText(string text)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Fill ?? Brushes.Black);
    }

    #endregion
}