using System;
using Ssz.Operator.Core.ControlsCommon.Trends.Converters;

namespace Ssz.Operator.Core.ControlsCommon.Trends;

[TemplatePart(Name = YAxis_PART, Type = typeof (Axis))]
[TemplatePart(Name = HorizontalScrollBar_PART, Type = typeof (ScrollBar))]
[TemplatePart(Name = VerticalScrollBar_PART, Type = typeof (ScrollBar))]
[TemplatePart(Name = DisplayValuesSlider_PART, Type = typeof (RangeBase))]
[TemplatePart(Name = DisplayValuesMoveRight_PART, Type = typeof (ButtonBase))]
[TemplatePart(Name = DisplayValuesMoveLeft_PART, Type = typeof (ButtonBase))]
public partial class CentumTrendsPlot : TrendsPlotView
{
    #region construction and destruction

    static CentumTrendsPlot()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof (CentumTrendsPlot),
            new FrameworkPropertyMetadata(typeof (CentumTrendsPlot)));
    }

    public CentumTrendsPlot()
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

    public ScrollBar HorizontalScrollBar { get; private set; }
    public ScrollBar VerticalScrollBar { get; private set; }

    public RangeBase DisplayValuesSlider { get; private set; }
    public ButtonBase DisplayValuesMoveRight { get; private set; }
    public ButtonBase DisplayValuesMoveLeft { get; private set; }

    public Axis YAxis { get; private set; }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        HorizontalScrollBar = (ScrollBar) GetTemplateChild(HorizontalScrollBar_PART);
        VerticalScrollBar = (ScrollBar) GetTemplateChild(VerticalScrollBar_PART);

        DisplayValuesSlider = (RangeBase) GetTemplateChild(DisplayValuesSlider_PART);
        DisplayValuesMoveLeft = (ButtonBase) GetTemplateChild(DisplayValuesMoveLeft_PART);
        DisplayValuesMoveRight = (ButtonBase) GetTemplateChild(DisplayValuesMoveRight_PART);

        YAxis = (Axis) GetTemplateChild(YAxis_PART);

        if (XAxis != null)
        {
            XAxis.SetBinding(Axis.AbsoluteMinimumProperty, new Binding("TotalDateRange.Minimum")
            {
                Converter = new DateTimeToDoubleConverter()
            });
            XAxis.SetBinding(Axis.AbsoluteMaximumProperty, new Binding("TotalDateRange.Maximum")
            {
                Converter = new DateTimeToDoubleConverter()
            });

            XAxis.StringFormat = "HH:mm:ss";
        }

        if (YAxis != null)
        {
            YAxis.SetBinding(Axis.AbsoluteMinimumProperty, new Binding("SelectedItem.YMinWithPadding"));
            YAxis.SetBinding(Axis.AbsoluteMaximumProperty,
                new Binding("SelectedItem.YMaxWithPadding") {TargetNullValue = 100.0});
            YAxis.SetBinding(Axis.TitleProperty, new Binding("SelectedItem.Source.HdaIdToDisplay"));

            YAxis.SetBinding(Axis.StringFormatProperty, new Binding("SelectedItem.Source.ValueFormat"));

            SetBinding(ColorProperty, new Binding("SelectedItem.Color"));
        }

        if (HorizontalScrollBar != null)
        {
            HorizontalScrollBar.ValueChanged += horizontalScrollBar_OnValueChanged;
            HorizontalScrollBar.Value = HorizontalScrollBar.Maximum;

            refreshHorizontalScrollBar();
        }

        if (VerticalScrollBar != null)
        {
            VerticalScrollBar.ValueChanged += verticalScrollBar_OnValueChanged;
            VerticalScrollBar.Value = (VerticalScrollBar.Minimum + VerticalScrollBar.Maximum)/2;
        }

        if (DisplayValuesSlider != null)
        {
            DisplayValuesSlider.ValueChanged += onDisplayValuesSliderValueChanged;
        }

        if (DisplayValuesMoveLeft != null)
        {
            DisplayValuesMoveLeft.Click += onDisplayValuesMoveLeftClicked;
        }

        if (DisplayValuesMoveRight != null)
        {
            DisplayValuesMoveRight.Click += onDisplayValuesMoveRightClicked;
        }

        SetBinding(VisibleValueRangeProperty, new Binding("SelectedItem.VisibleValueRange"));
        SetBinding(MinimumValueProperty, new Binding("SelectedItem.YMinWithPadding"));
        SetBinding(MaximumValueProperty, new Binding("SelectedItem.YMaxWithPadding"));

        refreshVisibleValuesRange();
    }

    #endregion

    #region protected functions

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == MinimumVisibleTimeProperty || e.Property == MaximumVisibleTimeProperty)
        {
            refreshHorizontalScrollBar();
            updateValuesUnderSliderPosition();
        }

        if (e.Property == MinimumValueProperty || e.Property == MaximumValueProperty)
        {
            refreshVisibleValuesRange();

            YAxis.MajorStep = visibleValueRange/5;
            YAxis.MinorStep = visibleValueRange/10;
        }

        if (e.Property == SelectedTrendProperty)
        {
            RefreshLines();
        }

        if (e.Property == ColorProperty)
        {
            var newColor = (Color) e.NewValue;

            YAxis.TitleColor = newColor;
            YAxis.InternalAxis.TextColor = newColor.ToOxyColor();
        }
    }

    #endregion

    #region private functions

    private void horizontalScrollBar_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (XAxis == null)
            return;

        var viewModel = DataContext as CentumTrendsViewModel;
        if (viewModel == null)
            return;

        double scrollbar01 = (HorizontalScrollBar.Value - HorizontalScrollBar.Minimum)/
                             (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);

        viewModel.OnHorizontalScrollChanged(scrollbar01);
    }

    private void verticalScrollBar_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        refreshVisibleValuesRange();
    }

    private void refreshVisibleValuesRange()
    {
        if (YAxis == null)
            return;

        if (VerticalScrollBar != null)
        {
            double scrollBarRange = VerticalScrollBar.Maximum - VerticalScrollBar.Minimum;
            double scrollBarValueFrom0To1 = (VerticalScrollBar.Value - VerticalScrollBar.Minimum)/
                                            scrollBarRange;

            YAxis.Minimum = minimumValue*scrollBarValueFrom0To1 +
                            (maximumValue - visibleValueRange)*(1 - scrollBarValueFrom0To1);

            YAxis.Maximum = (minimumValue + visibleValueRange)*scrollBarValueFrom0To1 +
                            maximumValue*(1 - scrollBarValueFrom0To1);

            double valuesRange = maximumValue - minimumValue;
            if (valuesRange > 1e-6)
            {
                VerticalScrollBar.ViewportSize = visibleValueRange/valuesRange*scrollBarRange;
            }
        }
    }

    private void refreshHorizontalScrollBar()
    {
        var viewModel = DataContext as CentumTrendsViewModel;
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
                                  CentumTrendsViewModel.TotalTimeRange.Ticks*
                                  (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);

            HorizontalScrollBar.ViewportSize = Math.Max(viewportSize, 1);
        }
    }

    private void updateValuesUnderSliderPosition()
    {
        var viewModel = DataContext as CentumTrendsViewModel;
        if (viewModel == null)
            return;

        double sliderValue0To1 = 1;
        if (DisplayValuesSlider != null && DisplayValuesSlider.Visibility == Visibility.Visible)
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

    private void onDisplayValuesSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        updateValuesUnderSliderPosition();
    }

    private void onDisplayValuesMoveRightClicked(object sender, RoutedEventArgs e)
    {
        if (DisplayValuesSlider != null)
            DisplayValuesSlider.Value += 1;
    }

    private void onDisplayValuesMoveLeftClicked(object sender, RoutedEventArgs e)
    {
        if (DisplayValuesSlider != null)
            DisplayValuesSlider.Value--;
    }

    private double visibleValueRange
    {
        get { return (double) GetValue(VisibleValueRangeProperty); }
    }

    private double minimumValue
    {
        get { return (double) GetValue(MinimumValueProperty); }
    }

    private double maximumValue
    {
        get { return (double) GetValue(MaximumValueProperty); }
    }

    #endregion

    #region private fields

    private static readonly DependencyProperty VisibleValueRangeProperty = DependencyProperty.Register(
        "VisibleValueRange",
        typeof (double),
        typeof (CentumTrendsPlot),
        new PropertyMetadata(null));

    private static readonly DependencyProperty MinimumValueProperty = DependencyProperty.Register(
        "MinimumValue",
        typeof (double),
        typeof (CentumTrendsPlot),
        new PropertyMetadata(null));

    private static readonly DependencyProperty MaximumValueProperty = DependencyProperty.Register(
        "MaximumValue",
        typeof (double),
        typeof (CentumTrendsPlot),
        new PropertyMetadata(null));

    private static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        "Color",
        typeof (Color),
        typeof (CentumTrendsPlot),
        new PropertyMetadata(null));

    #endregion
}