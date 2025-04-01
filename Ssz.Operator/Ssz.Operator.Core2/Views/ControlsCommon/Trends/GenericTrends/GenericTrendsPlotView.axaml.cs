using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;
using OxyPlot.Avalonia;
using Ssz.Operator.Core.ControlsCommon.Trends.Converters;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends;

[TemplatePart(Name = YAxis_PART, Type = typeof (Axis))]
[TemplatePart(Name = HorizontalScrollBar_PART, Type = typeof (ScrollBar))]
[TemplatePart(Name = VerticalScrollBar_PART, Type = typeof (ScrollBar))]
[TemplatePart(Name = DisplayValuesSlider_PART, Type = typeof (Range))]
[TemplatePart(Name = DisplayValuesMoveRight_PART, Type = typeof (Button))]
[TemplatePart(Name = DisplayValuesMoveLeft_PART, Type = typeof (Button))]
public partial class GenericTrendsPlotView : TrendsPlotView
{
    #region construction and destruction    

    public GenericTrendsPlotView()
    {
        InitializeComponent();
    }

    #endregion

    #region public functions

    public const string HorizontalScrollBar_PART = "HorizontalScrollBar";
    public const string VerticalScrollBar_PART = "VerticalScrollBar";
    public const string DisplayValuesSlider_PART = "DisplayValuesSlider";
    public const string DisplayValuesMoveRight_PART = "DisplayValues_MoveRight";
    public const string DisplayValuesMoveLeft_PART = "DisplayValues_MoveLeft";
    public const string YAxis_PART = "YAxis";

    public ScrollBar? HorizontalScrollBar { get; private set; }
    public ScrollBar? VerticalScrollBar { get; private set; }
    public RangeBase? DisplayValuesSlider { get; private set; }
    public Button? DisplayValuesMoveRight { get; private set; }
    public Button? DisplayValuesMoveLeft { get; private set; }
    public Axis? YAxis { get; private set; }

    #endregion

    #region protected functions

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        HorizontalScrollBar = e.NameScope.Find(HorizontalScrollBar_PART) as ScrollBar;
        VerticalScrollBar = e.NameScope.Find(VerticalScrollBar_PART) as ScrollBar;
        DisplayValuesSlider = e.NameScope.Find(DisplayValuesSlider_PART) as RangeBase;
        DisplayValuesMoveLeft = e.NameScope.Find(DisplayValuesMoveLeft_PART) as Button;
        DisplayValuesMoveRight = e.NameScope.Find(DisplayValuesMoveRight_PART) as Button;
        YAxis = e.NameScope.Find(YAxis_PART) as Axis;

        if (XAxis != null)
        {
            XAxis.Bind(Axis.AbsoluteMinimumProperty, new Binding("TotalDateRange.Minimum")
            {
                Converter = new DateTimeToDoubleConverter()
            });
            XAxis.Bind(Axis.AbsoluteMaximumProperty, new Binding("TotalDateRange.Maximum")
            {
                Converter = new DateTimeToDoubleConverter()
            });

            XAxis.StringFormat = "HH:mm:ss";
        }

        if (YAxis != null)
        {
            YAxis.Bind(Axis.AbsoluteMinimumProperty, new Binding("SelectedItem.YMinWithPadding"));
            YAxis.Bind(Axis.AbsoluteMaximumProperty,
                new Binding("SelectedItem.YMaxWithPadding") { TargetNullValue = 100.0 });
            YAxis.Bind(Axis.TitleProperty, new Binding("SelectedItem.Source.HdaIdToDisplay"));

            YAxis.Bind(Axis.StringFormatProperty, new Binding("SelectedItem.Source.ValueFormat"));

            Bind(ColorProperty, new Binding("SelectedItem.Color"));
        }

        if (HorizontalScrollBar != null)
        {
            HorizontalScrollBar.ValueChanged += HorizontalScrollBar_OnValueChanged;
            HorizontalScrollBar.Value = HorizontalScrollBar.Maximum;

            RefreshHorizontalScrollBar();
        }

        if (VerticalScrollBar != null)
        {
            VerticalScrollBar.ValueChanged += VerticalScrollBar_OnValueChanged;
            VerticalScrollBar.Value = (VerticalScrollBar.Minimum + VerticalScrollBar.Maximum) / 2;
        }

        if (DisplayValuesSlider != null)
        {
            DisplayValuesSlider.ValueChanged += OnDisplayValuesSliderValueChanged;
        }

        if (DisplayValuesMoveLeft != null)
        {
            DisplayValuesMoveLeft.Click += OnDisplayValuesMoveLeftClicked;
        }

        if (DisplayValuesMoveRight != null)
        {
            DisplayValuesMoveRight.Click += OnDisplayValuesMoveRightClicked;
        }

        Bind(VisibleValueRangeProperty, new Binding("SelectedItem.VisibleValueRange"));
        Bind(MinimumValueProperty, new Binding("SelectedItem.YMinWithPadding"));
        Bind(MaximumValueProperty, new Binding("SelectedItem.YMaxWithPadding"));

        RefreshVisibleValuesRange();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (YAxis is null)
            return;

        if (e.Property == MinimumVisibleTimeProperty || e.Property == MaximumVisibleTimeProperty)
        {
            RefreshHorizontalScrollBar();
            UpdateValuesUnderSliderPosition();
        }

        if (e.Property == MinimumValueProperty || e.Property == MaximumValueProperty)
        {
            RefreshVisibleValuesRange();

            YAxis.MajorStep = VisibleValueRange/5;
            YAxis.MinorStep = VisibleValueRange/10;
        }        

        if (e.Property == ColorProperty)
        {
            var newColor = (Color) e.NewValue!;

            YAxis.TitleColor = newColor;
            YAxis.InternalAxis.TextColor = newColor.ToOxyColor();
        }
    }

    #endregion

    #region private functions

    private static readonly AvaloniaProperty VisibleValueRangeProperty = AvaloniaProperty.Register<GenericTrendsPlotView, double>(
        nameof(VisibleValueRange));

    private static readonly AvaloniaProperty MinimumValueProperty = AvaloniaProperty.Register<GenericTrendsPlotView, double>(
        nameof(MinimumValue));

    private static readonly AvaloniaProperty MaximumValueProperty = AvaloniaProperty.Register<GenericTrendsPlotView, double>(
        nameof(MaximumValue));

    private static readonly AvaloniaProperty ColorProperty = AvaloniaProperty.Register<GenericTrendsPlotView, Color>(
        nameof(Color));

    private double VisibleValueRange
    {
        get { return (double)GetValue(VisibleValueRangeProperty)!; }
    }

    private double MinimumValue
    {
        get { return (double)GetValue(MinimumValueProperty)!; }
    }

    private double MaximumValue
    {
        get { return (double)GetValue(MaximumValueProperty)!; }
    }

    private Color Color
    {
        get { return (Color)GetValue(ColorProperty)!; }
    }    

    private void RefreshVisibleValuesRange()
    {
        if (YAxis == null)
            return;

        if (VerticalScrollBar != null)
        {
            double scrollBarRange = VerticalScrollBar.Maximum - VerticalScrollBar.Minimum;
            double scrollBarValueFrom0To1 = (VerticalScrollBar.Value - VerticalScrollBar.Minimum)/
                                            scrollBarRange;

            YAxis.Minimum = MinimumValue*scrollBarValueFrom0To1 +
                            (MaximumValue - VisibleValueRange)*(1 - scrollBarValueFrom0To1);

            YAxis.Maximum = (MinimumValue + VisibleValueRange)*scrollBarValueFrom0To1 +
                            MaximumValue*(1 - scrollBarValueFrom0To1);

            double valuesRange = MaximumValue - MinimumValue;
            if (valuesRange > 1e-6)
            {
                VerticalScrollBar.ViewportSize = VisibleValueRange/valuesRange*scrollBarRange;
            }
        }
    }

    private void RefreshHorizontalScrollBar()
    {
        var viewModel = DataContext as GenericTrendsViewModel;
        if (viewModel == null)
            return;

        if (HorizontalScrollBar != null)
        {
            double newPosition = new DateRange(
                viewModel.TotalDateRange.Minimum,
                viewModel.TotalDateRange.Maximum - viewModel.VisibleDateRange.Range)
                .Percentage(viewModel.VisibleDateRange.Minimum);

            HorizontalScrollBar.Value =
                HorizontalScrollBar.Minimum +
                newPosition*(HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);

            double viewportSize = (double) (viewModel.VisibleDateRange.Range).Ticks/
                                  GenericTrendsViewModel.TotalTimeRange.Ticks*
                                  (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);

            HorizontalScrollBar.ViewportSize = Math.Max(viewportSize, 1);
        }
    }

    private void UpdateValuesUnderSliderPosition()
    {
        var viewModel = DataContext as GenericTrendsViewModel;
        if (viewModel == null)
            return;

        double sliderValue0To1 = 1;
        if (DisplayValuesSlider != null && DisplayValuesSlider.IsVisible)
            sliderValue0To1 = (DisplayValuesSlider.Value - DisplayValuesSlider.Minimum)/
                              (DisplayValuesSlider.Maximum - DisplayValuesSlider.Minimum);

        if (Math.Abs(sliderValue0To1 - 1) < 1e-6)
        {
            viewModel.DisplayCurrentTrendValues();
        }
        else if (!double.IsNaN(sliderValue0To1))
        {
            DateTime time = viewModel.VisibleDateRange.Interpolate(sliderValue0To1);
            viewModel.DisplayValuesAtTime(time);
        }
    }

    private void HorizontalScrollBar_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (XAxis is null || HorizontalScrollBar is null)
            return;

        var viewModel = DataContext as GenericTrendsViewModel;
        if (viewModel is null)
            return;

        double scrollbar01 = (HorizontalScrollBar.Value - HorizontalScrollBar.Minimum) /
                             (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);

        viewModel.OnHorizontalScrollChanged(scrollbar01);
    }

    private void VerticalScrollBar_OnValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        RefreshVisibleValuesRange();
    }

    private void OnDisplayValuesSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        UpdateValuesUnderSliderPosition();
    }

    private void OnDisplayValuesMoveRightClicked(object? sender, RoutedEventArgs e)
    {
        if (DisplayValuesSlider != null)
            DisplayValuesSlider.Value += 1;
    }

    private void OnDisplayValuesMoveLeftClicked(object? sender, RoutedEventArgs e)
    {
        if (DisplayValuesSlider != null)
            DisplayValuesSlider.Value--;
    }   

    #endregion    
}