using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsCommon;

public class ChartTickBar : Control
{
    #region construction and destruction

    public ChartTickBar()
    {
        IsHitTestVisible = false;
    }

    static ChartTickBar()
    {
        AffectsRender<ChartTickBar>(
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
            TickStrokeThicknessProperty,
            LabelsBeginIsVisibleProperty,
            LabelsEndIsVisibleProperty,
            LabelsFillProperty);
    }

    #endregion

    #region public functions Ч свойства из TickBar (AddOwner)

    public static readonly StyledProperty<double> MinimumProperty =
        TickBar.MinimumProperty.AddOwner<ChartTickBar>();

    public static readonly StyledProperty<double> MaximumProperty =
        TickBar.MaximumProperty.AddOwner<ChartTickBar>();

    public static readonly StyledProperty<double> TickFrequencyProperty =
        TickBar.TickFrequencyProperty.AddOwner<ChartTickBar>();

    public static readonly StyledProperty<TickBarPlacement> PlacementProperty =
        TickBar.PlacementProperty.AddOwner<ChartTickBar>();

    public static readonly StyledProperty<IBrush?> FillProperty =
        TickBar.FillProperty.AddOwner<ChartTickBar>();

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

    public static readonly StyledProperty<FontFamily> FontFamilyProperty =
        AvaloniaProperty.Register<ChartTickBar, FontFamily>(
            nameof(FontFamily),
            defaultValue: new FontFamily("Courier New"));

    public static readonly StyledProperty<FontStyle> FontStyleProperty =
        AvaloniaProperty.Register<ChartTickBar, FontStyle>(
            nameof(FontStyle),
            defaultValue: FontStyle.Normal);

    public static readonly StyledProperty<FontWeight> FontWeightProperty =
        AvaloniaProperty.Register<ChartTickBar, FontWeight>(
            nameof(FontWeight),
            defaultValue: FontWeight.Normal);

    public static readonly StyledProperty<FontStretch> FontStretchProperty =
        AvaloniaProperty.Register<ChartTickBar, FontStretch>(
            nameof(FontStretch),
            defaultValue: FontStretch.Normal);

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<ChartTickBar, double>(
            nameof(FontSize),
            defaultValue: 14.0);

    public static readonly StyledProperty<string> FormatProperty =
        AvaloniaProperty.Register<ChartTickBar, string>(
            nameof(Format),
            defaultValue: "F02");

    public static readonly StyledProperty<string> EngUnitProperty =
        AvaloniaProperty.Register<ChartTickBar, string>(
            nameof(EngUnit),
            defaultValue: "");

    public static readonly StyledProperty<double> TickStrokeThicknessProperty =
        AvaloniaProperty.Register<ChartTickBar, double>(
            nameof(TickStrokeThickness),
            defaultValue: 1.0);

    public static readonly StyledProperty<bool> LabelsBeginIsVisibleProperty =
        AvaloniaProperty.Register<ChartTickBar, bool>(
            nameof(LabelsBeginIsVisible),
            defaultValue: false);

    public static readonly StyledProperty<bool> LabelsEndIsVisibleProperty =
        AvaloniaProperty.Register<ChartTickBar, bool>(
            nameof(LabelsEndIsVisible),
            defaultValue: false);

    public static readonly StyledProperty<IBrush?> LabelsFillProperty =
        AvaloniaProperty.Register<ChartTickBar, IBrush?>(
            nameof(LabelsFill),
            defaultValue: null);

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

    public double TickStrokeThickness
    {
        get => GetValue(TickStrokeThicknessProperty);
        set => SetValue(TickStrokeThicknessProperty, value);
    }

    public bool LabelsBeginIsVisible
    {
        get => GetValue(LabelsBeginIsVisibleProperty);
        set => SetValue(LabelsBeginIsVisibleProperty, value);
    }

    public bool LabelsEndIsVisible
    {
        get => GetValue(LabelsEndIsVisibleProperty);
        set => SetValue(LabelsEndIsVisibleProperty, value);
    }

    public IBrush? LabelsFill
    {
        get => GetValue(LabelsFillProperty);
        set => SetValue(LabelsFillProperty, value);
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

        if (Placement == TickBarPlacement.Left || Placement == TickBarPlacement.Right)
        {
            var tickDelta = Bounds.Height * TickFrequency / delta;
            if (tickDelta > 1)
            {
                var v = Minimum;
                for (var y = Bounds.Height; y > -1; y -= tickDelta)
                {
                    DrawTick(dc, 0, y, Bounds.Width, y);

                    if (LabelsBeginIsVisible || LabelsEndIsVisible)
                    {
                        FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                        var margin = formattedText.Height * 0.2;
                        if (y - margin > 0 && y + margin + formattedText.Height < Bounds.Height)
                        {
                            if (LabelsBeginIsVisible)
                                dc.DrawText(formattedText, new Point(margin, y + margin));
                            if (LabelsEndIsVisible)
                                dc.DrawText(formattedText,
                                    new Point(Bounds.Width - margin - formattedText.Width, y + margin));
                        }

                        v += TickFrequency;
                    }
                }
            }

            return;
        }

        if (Placement == TickBarPlacement.Bottom || Placement == TickBarPlacement.Top)
        {
            var tickDelta = Bounds.Width * TickFrequency / delta;
            if (tickDelta > 1)
            {
                var v = Minimum;
                for (double x = 0; x < Bounds.Width + 1; x += tickDelta)
                {
                    DrawTick(dc, x, 0, x, Bounds.Height);

                    if (LabelsBeginIsVisible || LabelsEndIsVisible)
                    {
                        FormattedText formattedText = GetFormattedText(GetValueWithEngUnit(v));
                        var margin = formattedText.Height * 0.2;
                        if (x - margin > 0 && x + margin + formattedText.Width < Bounds.Width)
                        {
                            if (LabelsBeginIsVisible)
                                dc.DrawText(formattedText, new Point(x + margin, margin));
                            if (LabelsEndIsVisible)
                                dc.DrawText(formattedText,
                                    new Point(x + margin, Bounds.Height - margin - formattedText.Height));
                        }

                        v += TickFrequency;
                    }
                }
            }

            return;
        }

        base.Render(dc);
    }

    #endregion

    #region private functions

    private void DrawTick(DrawingContext dc, double x1, double y1, double x2, double y2)
    {
        dc.DrawLine(new Pen(Fill, TickStrokeThickness),
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
            LabelsFill ?? Brushes.Black);
    }

    #endregion
}
