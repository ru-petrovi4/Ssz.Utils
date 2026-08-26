using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using OxyPlot;
using OxyPlot.Avalonia;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    /// <summary>
    ///     Interaction logic for TrendsPlotView.axaml
    /// </summary>
    [TemplatePart(Name = HorizontalScrollBar_PART, Type = typeof (ScrollBar))]
    [TemplatePart(Name = AdditionalGrid_PART, Type = typeof (Grid))]
    [TemplatePart(Name = YAxis_PART, Type = typeof(Axis))]
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
        public const string AdditionalGrid_PART = "AdditionalGrid";
        public const string YAxis_PART = "YAxis";
        
        public ScrollBar? HorizontalScrollBar { get; private set; }
        public Grid? AdditionalGrid { get; private set; }
        public Axis? YAxis { get; private set; }

        public bool DisableMouseZoom = false;
        //public ValuesControl RightCurrentValuesControl { get; } = new ValuesControl();        

        public event Action TimeZoomChanged = delegate { };

        public static readonly AvaloniaProperty SelectedItemColorProperty = AvaloniaProperty.Register<GenericTrendsPlotView, Color>(
            nameof(SelectedItemColor), Colors.Red);

        public Color SelectedItemColor
        {
            get { return (Color)GetValue(SelectedItemColorProperty)!; }
        }        

        public void ResetPanningAndZooming()
        {
            var viewModel = (GenericTrendsViewModel) DataContext!;
            viewModel.ResetPanAndZoom();

            ResetYAxesOffsets();
            OnRemoveAllRulers(null, new RoutedEventArgs());
        }

        public void ZoomSelectedValueAxisIn()
        {
            if (Plot is null)
                return;

            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedItem);
            if (axis is null)
                return;

            axis.ZoomIn();
        }

        public void ZoomSelectedValueAxisOut()
        {
            if (Plot is null)
                return;

            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedItem);
            if (axis is null)
                return;

            axis.ZoomOut();
        }

        public void GetSelectedValueAxisMinimumAndMaximum(out double minimum, out double maximum)
        {
            minimum = 0;
            maximum = 100;

            if (Plot is null)
                return;

            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedItem);
            if (axis is null)
                return;

            minimum = axis.InternalAxis.ActualMinimum;
            maximum = axis.InternalAxis.ActualMaximum;
        }

        public void SetSelectedValueAxisMinimumAndMaximum(double minimum, double maximum)
        {
            if (Plot is null)
                return;

            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedItem);
            if (axis is null)
                return;

            axis.SetMinimumAndMaximum(minimum, maximum);
        }

        public void CenterPlotAroundDisplayValueSliderPosition()
        {
            var viewModel = (GenericTrendsViewModel) DataContext!;

            viewModel.CenterMinAndMaxAroundCurrentDisplayedValue();
        }

        public void WriteSettings(TrendsGroupConfiguration configuration)
        {
            if (Plot is null)
                return;

            // Every WPF brush derived from SolidColorBrush; Avalonia parses a colour string into an
            // ImmutableSolidColorBrush, which does not, so the interface is what can be matched.
            if (Plot.PlotAreaBackground is ISolidColorBrush plotAreaBackgroundBrush)
                configuration.PlotAreaBackgroundColor = plotAreaBackgroundBrush.Color;
            //configuration.PlotBackgroundColor = ((SolidColorBrush) Plot.Background).Color;

            configuration.DsTrendItemsCollection = Plot.Axes.Skip(2).Select(a => ((TrendViewModel)a.DataContext!).Source.DsTrendItem).ToArray();
        }

        public void ReadSettings(TrendsGroupConfiguration configuration)
        {
            if (Plot is null)
                return;

            if (configuration.PlotAreaBackgroundColor != null)
                Plot.PlotAreaBackground = new SolidColorBrush(configuration.PlotAreaBackgroundColor.Value);
            if (configuration.PlotBackgroundColor != null)
                Plot.Background = new SolidColorBrush(configuration.PlotBackgroundColor.Value);

            if (configuration.DsTrendItemsCollection != null)
            {
                var viewModel = (TrendsViewModel) DataContext!;
                viewModel.Display(configuration.DsTrendItemsCollection);
                /*
                for (int i = 0; i < Plot.Axes.Count - 1; ++i)
                {
                    TrendConfiguration trendConfiguration = configuration.DsTrendItemsCollection[i];

                    if (trendConfiguration.Minimum != null && trendConfiguration.Maximum != null)
                    {
                        ((GenericAxis) Plot.Axes[i + 1]).SetMinimumAndMaximum(
                            trendConfiguration.Minimum.Value,
                            trendConfiguration.Maximum.Value);
                    }
                }*/
            }
        }

        #endregion

        #region protected functions

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);            

            HorizontalScrollBar = e.NameScope.Find(HorizontalScrollBar_PART) as ScrollBar;
            AdditionalGrid = e.NameScope.Find(AdditionalGrid_PART) as Grid;
            //AdditionalGrid.Children.Add(RightCurrentValuesControl);
            //RightCurrentValuesControl.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;

            if (Plot is not null)
            {
                Plot.ApplyTemplate();
                Plot.ActualController.UnbindMouseDown(OxyMouseButton.Left);
                Plot.ActualController.UnbindMouseDown(OxyMouseButton.Right);

                //Plot.ActualController.BindMouseDown(OxyMouseButton.Left,
                //    new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
                //    {
                //        if (!Plot.ActualModel.PlotArea.Contains(args.Position))
                //            return;

                //        if (!DisableMouseZoom)
                //        {
                //            Plot.ActualController.AddMouseManipulator(view, new ZoomManipulator(view, this), args);
                //        }
                //    }));
                Plot.ActualController.BindMouseDown(OxyMouseButton.Left, OxyModifierKeys.None, 1,
                    new DelegatePlotCommand<OxyMouseDownEventArgs>((view, controller, args) =>
                    {
                        if (!Plot.ActualModel.PlotArea.Contains(args.Position))
                            return;

                        TryAddRuler(args.Position);
                    }));
            }

            ResetYAxesOffsets();

            YAxis = e.NameScope.Find(YAxis_PART) as Axis;

            UpdateSelectedItemBindings();
        }

        protected override void RefreshLines()
        {
            if (Plot is null)
                return;

            if (Items is null)
                return;

            List<TrendViewModel> trendItems = Items.ToList();

            ClearLines();

            int nItem = 0;
            foreach (TrendViewModel trendItem in trendItems)
            {
                var axis = new GenericYAxis
                {
                    PositionTier = Plot.Axes.Count - 2,
                    AxislineStyle = LineStyle.Solid,
                    IsZoomEnabled = false,
                    DataContext = trendItem,
                    Key = nItem.ToString(CultureInfo.InvariantCulture)
                };
                axis.Position = OxyPlot.Axes.AxisPosition.Left;                

                Plot.Axes.Add(axis);
                nItem ++;
            }

            foreach (TrendViewModel trendItem in ItemsInDisplayOrder(trendItems))
            {
                LineSeries series = AddLine(trendItem);
                series.YAxisKey = trendItems.IndexOf(trendItem).ToString(CultureInfo.InvariantCulture);
            }

            // The per-trend axes have just been recreated, so re-point everything that follows them.
            UpdateSelectedItemBindings();
        }

        protected override void ClearLines()
        {
            base.ClearLines();

            List<Axis> allVerticalAxes = Plot!.Axes.Skip(2).ToList();
            allVerticalAxes.ForEach(axis => Plot.Axes.Remove(axis));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // Series are rebuilt in RefreshLines() by the base class when SelectedItem changes,
            // in display order and with the proper stroke thickness, so nothing to do here.

            if (e.Property == MinimumVisibleTimeProperty ||
                e.Property == MaximumVisibleTimeProperty)
            {
                UpdateValueLabels();
            }

            if (e.Property == SelectedItemProperty)
            {
                UpdateSelectedItemBindings();
            }

            if (e.Property == SelectedItemColorProperty && YAxis is not null)
            {
                // These have to go through the OxyPlot.Avalonia wrapper properties: values written
                // straight to InternalAxis are overwritten on the next SynchronizeProperties().
                YAxis.TitleColor = SelectedItemColor;
                YAxis.TextColor = SelectedItemColor;
                YAxis.TicklineColor = SelectedItemColor;
                YAxis.AxislineColor = SelectedItemColor;

                Plot?.InvalidatePlot(false);
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Y axis properties follow the selected trend.
        ///     Binding them through the "SelectedItem.*" path is not an option: while no trend is selected
        ///     (an empty TrendGroupControl, or one before Jump(...) is called) the path breaks on the null
        ///     SelectedItem, which Avalonia reports as a binding error. TargetNullValue does not help here,
        ///     it only covers a null source value, not a broken path, so the axis ends up with UnsetValue.
        ///     Instead the selected trend is used as the binding source, and the defaults are applied
        ///     directly when there is nothing selected.
        /// </summary>
        private void UpdateSelectedItemBindings()
        {
            foreach (var binding in _selectedItemBindings)
                binding.Dispose();
            _selectedItemBindings.Clear();

            var selectedItem = SelectedItem as GenericTrendViewModel;

            // The alarm limits of the selected trend arrive asynchronously, so they are followed.
            if (_selectedTrend is not null)
                _selectedTrend.PropertyChanged -= OnSelectedTrendPropertyChanged;
            _selectedTrend = selectedItem?.Source;
            if (_selectedTrend is not null)
                _selectedTrend.PropertyChanged += OnSelectedTrendPropertyChanged;

            // The selected trend is drawn on its own hidden axis; the visible axis mirrors that one.
            if (_selectedYAxis is not null)
                _selectedYAxis.PropertyChanged -= OnSelectedYAxisPropertyChanged;
            _selectedYAxis = Plot?.Axes.OfType<GenericYAxis>()
                .FirstOrDefault(a => ReferenceEquals(a.DataContext, selectedItem));
            if (_selectedYAxis is not null)
                _selectedYAxis.PropertyChanged += OnSelectedYAxisPropertyChanged;

            RefreshAlarmLimitAnnotations();

            if (selectedItem is null)
            {
                SetValue(SelectedItemColorProperty, Colors.Black);

                if (YAxis is not null)
                {
                    // Same values the control template declares for YAxis: a binding set at local value
                    // priority has overwritten them, so they have to be restored explicitly.
                    YAxis.AbsoluteMinimum = 0.0;
                    YAxis.AbsoluteMaximum = 100.0;
                    YAxis.Minimum = 0.0;
                    YAxis.Maximum = 100.0;
                    YAxis.MajorStep = 10.0;
                    YAxis.MinorStep = 2.5;
                    YAxis.StringFormat = @"F02";
                }

                return;
            }

            _selectedItemBindings.Add(this.Bind(SelectedItemColorProperty,
                new Binding(nameof(GenericTrendViewModel.Color)) { Source = selectedItem }));

            if (YAxis is null)
                return;

            _selectedItemBindings.Add(YAxis.Bind(Axis.StringFormatProperty,
                new Binding(nameof(GenericTrendViewModel.ValueFormat)) { Source = selectedItem }));

            SyncVisibleYAxisWithSelectedTrend();
        }

        /// <summary>
        ///     Copies the range of the selected trend's hidden axis onto the visible one.
        ///     Taking it from the hidden axis rather than from the view model is what keeps the labels in
        ///     step with the value zoom buttons, which zoom that hidden axis.
        /// </summary>
        private void SyncVisibleYAxisWithSelectedTrend()
        {
            if (YAxis is null || _selectedYAxis is null)
                return;

            double minimum = _selectedYAxis.Minimum;
            double maximum = _selectedYAxis.Maximum;
            if (Double.IsNaN(minimum) || Double.IsNaN(maximum) || minimum >= maximum)
                return;

            // The hidden axis is not restricted, so the visible one must not be either - otherwise
            // OxyPlot clamps it back and zooming out does nothing.
            YAxis.AbsoluteMinimum = Double.MinValue;
            YAxis.AbsoluteMaximum = Double.MaxValue;
            YAxis.Minimum = minimum;
            YAxis.Maximum = maximum;
            YAxis.MajorStep = (maximum - minimum) / 10;
            YAxis.MinorStep = (maximum - minimum) / 50;

            Plot?.InvalidatePlot(false);
        }

        private void OnSelectedYAxisPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Axis.MinimumProperty || e.Property == Axis.MaximumProperty)
                SyncVisibleYAxisWithSelectedTrend();
        }

        /// <summary>
        ///     Draws the alarm limits of the selected trend, the way the WPF original did: three dashed
        ///     pairs over the selected trend - the scale itself (white), the Lo/Hi alarm range (yellow)
        ///     and the LoLo/HiHi block range (red). A limit that is not set, or lies outside the scale,
        ///     collapses onto the corresponding scale bound.
        /// </summary>
        private void RefreshAlarmLimitAnnotations()
        {
            if (Plot is null)
                return;

            foreach (var annotation in _alarmLimitAnnotations)
                Plot.Annotations.Remove(annotation);
            _alarmLimitAnnotations.Clear();

            var selectedItem = SelectedItem;
            if (selectedItem is null)
                return;

            Trend trend = selectedItem.Source;
            if (Double.IsNaN(trend.YMin) || Double.IsNaN(trend.YMax) || trend.YMin >= trend.YMax)
                return;

            // The limits are drawn on the scale of the selected trend, i.e. on its own hidden axis.
            string? yAxisKey = Plot.Axes
                .FirstOrDefault(a => ReferenceEquals(a.DataContext, selectedItem))?.Key;
            if (yAxisKey is null)
                return;

            AddAlarmLimitAnnotation(yAxisKey, trend.YMin, Colors.White);
            AddAlarmLimitAnnotation(yAxisKey, trend.YMax, Colors.White);
            AddAlarmLimitAnnotation(yAxisKey, LimitOrBound(trend.LoAlarmLimit, trend, trend.YMin), Colors.Yellow);
            AddAlarmLimitAnnotation(yAxisKey, LimitOrBound(trend.HiAlarmLimit, trend, trend.YMax), Colors.Yellow);
            AddAlarmLimitAnnotation(yAxisKey, LimitOrBound(trend.LoLoAlarmLimit, trend, trend.YMin), Colors.Red);
            AddAlarmLimitAnnotation(yAxisKey, LimitOrBound(trend.HiHiAlarmLimit, trend, trend.YMax), Colors.Red);

            Plot.InvalidatePlot(false);
        }

        private static double LimitOrBound(double limit, Trend trend, double bound)
        {
            return !Double.IsNaN(limit) && limit > trend.YMin && limit < trend.YMax
                ? limit
                : bound;
        }

        private void AddAlarmLimitAnnotation(string yAxisKey, double y, Color color)
        {
            var annotation = new LineAnnotation
            {
                Type = OxyPlot.Annotations.LineAnnotationType.Horizontal,
                Y = y,
                YAxisKey = yAxisKey,
                Color = color,
                LineStyle = LineStyle.Dash,
                StrokeThickness = 2,
                ClipByYAxis = false
            };

            Plot!.Annotations.Add(annotation);
            _alarmLimitAnnotations.Add(annotation);
        }

        private void OnSelectedTrendPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            // The scale and the limits come from the data engine asynchronously.
            if (e.Property == Trend.YMinProperty ||
                e.Property == Trend.YMaxProperty ||
                e.Property == Trend.LoAlarmLimitProperty ||
                e.Property == Trend.HiAlarmLimitProperty ||
                e.Property == Trend.LoLoAlarmLimitProperty ||
                e.Property == Trend.HiHiAlarmLimitProperty)
            {
                RefreshAlarmLimitAnnotations();
            }
        }

        private void ResetYAxesOffsets()
        {
            //foreach (Axis yAxis in Plot.Axes.Skip(1))
            //{
            //    var trendItem = (TrendViewModel) yAxis.DataContext;

            //    yAxis.InternalAxis.Reset();

            //    yAxis.Minimum = trendItem.Source.YMin - (trendItem.Source.YMax - trendItem.Source.YMin)/2;
            //    yAxis.Maximum = trendItem.Source.YMax + (trendItem.Source.YMax - trendItem.Source.YMin)/2;
            //}
        }

        private void UpdateValueLabels()
        {
            if (AdditionalGrid is null)
                return;

            var viewModel = DataContext as TrendsViewModel;
            if (viewModel is null)
                return;

            foreach (var child in AdditionalGrid.Children)
            {
                UpdateRulerValues(child as Slider);
            }
        }

        private void UpdateRulerValues(Slider? ruler)
        {
            if (ruler is null)
                return;

            var viewModel = DataContext as TrendsViewModel;
            if (viewModel is null)
                return;

            var valuesControl = TreeHelper.FindChild<ValuesControl>(ruler, vc => vc.Name == "ValuesControl");
            if (valuesControl is null)
                return;

            double sliderValue0To1 = ruler.Value / 100;
            if (!double.IsNaN(sliderValue0To1))
            {
                DateTime time = viewModel.VisibleDateRange.Interpolate(sliderValue0To1);

                var dataValues = new List<string>();
                dataValues.Add(new Any(true).ValueAsString(true));
                dataValues.Add(new Any(time).ValueAsString(true));
                foreach (TrendViewModel trendViewModel in viewModel.Items)
                {
                    var value = trendViewModel.GetValue(time);
                    if (value is null || Double.IsNaN(value.Value)) continue;
                    dataValues.Add(new Any(trendViewModel.Color).ValueAsString(false, trendViewModel.Source.ValueFormat));
                    dataValues.Add(new Any(value.Value).ValueAsString(true, trendViewModel.Source.ValueFormat));
                }

                valuesControl.Data = CsvHelper.FormatForCsv(",", dataValues);
            }
            else
            {
                valuesControl.Data = @"";
            }
        }

        private void Plot_OnLayoutUpdated(object sender, EventArgs e)
        {
            if (AdditionalGrid is null)
                return;

            if (Plot?.ActualModel is not null)
            {
                Canvas.SetLeft(AdditionalGrid, Plot.ActualModel.PlotArea.Left);
                Canvas.SetTop(AdditionalGrid, Plot.ActualModel.PlotArea.Top);
                AdditionalGrid.Width = Plot.ActualModel.PlotArea.Width;
                AdditionalGrid.Height = Plot.ActualModel.PlotArea.Height;
            }
        }

        private void TryAddRuler(ScreenPoint position)
        {
            if (AdditionalGrid is null)
                return;

            double valueXPercent = 100 * (position.X - Plot!.ActualModel.PlotArea.Left) / Plot.ActualModel.PlotArea.Width;

            var ruler = new Slider()
            {
                Theme = Resources["RulerSliderStyle"] as ControlTheme,                
                Value = valueXPercent                
            };
            ruler.ValueChanged += (s, e) => UpdateRulerValues(s as Slider);
            ruler.Loaded += (s, e) => UpdateRulerValues(s as Slider);
            AdditionalGrid.Children.Add(ruler);
        }        

        private void OnRemoveAllRulers(object? sender, RoutedEventArgs e)
        {
            if (AdditionalGrid is null)
                return;

            foreach (var ruler in AdditionalGrid.Children.OfType<Slider>().ToArray())
            {
                AdditionalGrid.Children.Remove(ruler);
            }    
            //Plot!.HideZoomRectangle();

            //if (_completedZoomRect_DataPoint0 is null || _completedZoomRect_DataPoint1 is null)
            //    return;

            //var minimumVisibleTime = new DateTime(DateTimeAxis.ToDateTime(_completedZoomRect_DataPoint0.Value.X).Ticks, DateTimeKind.Local);
            //var maximumVisibleTime = new DateTime(DateTimeAxis.ToDateTime(_completedZoomRect_DataPoint1.Value.X).Ticks, DateTimeKind.Local);

            //if (maximumVisibleTime - minimumVisibleTime > TimeSpan.FromSeconds(10))
            //{
            //    var viewModel = (TrendsViewModel)DataContext;
            //    viewModel.Zoom(minimumVisibleTime, maximumVisibleTime);
            //    TimeZoomChanged();
            //}
        }

        #endregion

        #region private fields

        private DataPoint? _completedZoomRect_DataPoint0;
        private DataPoint? _completedZoomRect_DataPoint1;

        private readonly List<IDisposable> _selectedItemBindings = new();

        private readonly List<LineAnnotation> _alarmLimitAnnotations = new();

        private Trend? _selectedTrend;

        private GenericYAxis? _selectedYAxis;

        #endregion

        private class GenericYAxis : LinearAxis
        {
            #region construction and destruction

            public GenericYAxis()
            {
                // The trend scale (Trend.YMin/YMax) arrives from the data engine asynchronously and is
                // NaN to begin with. An axis left at NaN gets auto-scaled by OxyPlot to whatever the
                // empty series suggests, and the trend is then drawn far outside the plot area, i.e.
                // invisible. So the axis always starts from a usable range.
                Minimum = DefaultMinimum;
                Maximum = DefaultMaximum;

                Bind(YMinProperty, new Binding("Source.YMin"));
                Bind(YMaxProperty, new Binding("Source.YMax"));
                IsAxisVisible = false;
            }

            #endregion

            #region public functions

            public void ZoomIn()
            {
                Zoom(1/ValueZoomCoefficient);
            }

            public void ZoomOut()
            {
                Zoom(1*ValueZoomCoefficient);
            }

            public void SetMinimumAndMaximum(double minimum, double maximum)
            {
                InternalAxis.Reset();

                _minMaxOverriden = true;

                Minimum = minimum;
                Maximum = maximum;

                InternalAxis.Minimum = minimum;
                InternalAxis.Maximum = maximum;
            }

            public double GetMinimumToDisplay()
            {
                double max = (Maximum * (1 + YAxisCoefficient) + Minimum * YAxisCoefficient) / (1 + 2 * YAxisCoefficient);
                return Minimum + Maximum - max;
            }

            public double GetMaximumToDisplay()
            {
                return (Maximum * (1 + YAxisCoefficient) + Minimum * YAxisCoefficient) / (1 + 2 * YAxisCoefficient);
            }

            public double GetMiddleToDisplay()
            {
                return (Minimum + Maximum) / 2;
            }

            #endregion

            #region protected functions

            protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
            {
                base.OnPropertyChanged(e);

                if (e.Property != YMinProperty && e.Property != YMaxProperty)
                    return;

                if (_minMaxOverriden)
                    return;

                if (YMin < YMax)
                {
                    Minimum = YMin - (YMax - YMin) * YAxisCoefficient;
                    Maximum = YMax + (YMax - YMin) * YAxisCoefficient;
                }
                else
                {
                    // Scale not known (yet): keep a usable range instead of letting OxyPlot auto-scale.
                    Minimum = DefaultMinimum;
                    Maximum = DefaultMaximum;
                }
            }

            #endregion

            #region private functions

            private static readonly AvaloniaProperty YMinProperty = AvaloniaProperty.Register<GenericYAxis, double>(
                nameof(YMin));

            private static readonly AvaloniaProperty YMaxProperty = AvaloniaProperty.Register<GenericYAxis, double>(
                nameof(YMax));

            private double YMin
            {
                get { return (double)GetValue(YMinProperty)!; }
            }

            private double YMax
            {
                get { return (double)GetValue(YMaxProperty)!; }
            }

            private void Zoom(double zoomCoefficient)
            {
                double center = (InternalAxis.ActualMinimum + InternalAxis.ActualMaximum)/2;
                double height = (InternalAxis.ActualMaximum - InternalAxis.ActualMinimum);

                InternalAxis.Reset();

                Minimum = center - height/2*zoomCoefficient;
                Maximum = center + height/2*zoomCoefficient;
            }

            #endregion

            #region private fields            

            private bool _minMaxOverriden;
            private const double ValueZoomCoefficient = 1.1; // 10%
            private const double DefaultMinimum = 0.0;
            private const double DefaultMaximum = 100.0;
            /// <summary>
            ///     Must match the padding of GenericTrendViewModel.AxisMinimumWithPadding /
            ///     AxisMaximumWithPadding, which is what the visible Y axis is bound to. Otherwise the
            ///     trend is drawn on a different scale than the one its labels show.
            /// </summary>
            private const double YAxisCoefficient = 0.01; // 1%

            #endregion
        }

        private class ZoomManipulator : MouseManipulator
        {
            #region construction and destruction

            public ZoomManipulator(IPlotView plotView, GenericTrendsPlotView trendsPlotView) :
                base(plotView)
            {
                _trendsPlotView = trendsPlotView;
            }

            #endregion

            #region public functions

            public override void Delta(OxyMouseEventArgs args)
            {
                if (_trendsPlotView.Plot is not null)
                    _trendsPlotView.Plot.ZoomRectangleTemplate = (ControlTemplate)_trendsPlotView.Resources["ZoomRectangleTemplate"]!;

                OxyRect plotArea = PlotView.ActualModel.PlotArea;
                ScreenPoint position = ValidatePosition(plotArea, args.Position);
                OxyRect zoomOxyRect = GetZoomOxyRect(position, plotArea);                
                PlotView.ShowZoomRectangle(zoomOxyRect);

                args.Handled = true;
            }

            public override void Completed(OxyMouseEventArgs args)
            {
                //Application.Current.Dispatcher.Invoke(() =>
                //    _aspenTrendsPlotView.DisplayValuesSlider.IsHitTestVisible = true);
                if (_trendsPlotView.Plot is not null)
                    _trendsPlotView.Plot.ZoomRectangleTemplate = (ControlTemplate)_trendsPlotView.Resources["CompletedZoomRectangleTemplate"]!;

                OxyRect plotArea = PlotView.ActualModel.PlotArea;
                ScreenPoint position = ValidatePosition(plotArea, args.Position);
                OxyRect zoomOxyRect = GetZoomOxyRect(position, plotArea);
                PlotView.ShowZoomRectangle(zoomOxyRect);

                // TODO
                //_trendsPlotView.Plot?.PointerCaptureLost();

                if (zoomOxyRect.Width > 50)
                {
                    _trendsPlotView._completedZoomRect_DataPoint0 = InverseTransform(zoomOxyRect.Left, zoomOxyRect.Top);
                    _trendsPlotView._completedZoomRect_DataPoint1 = InverseTransform(zoomOxyRect.Right, zoomOxyRect.Bottom);
                }
                
                args.Handled = true;
            }

            #endregion

            #region private functions

            private static ScreenPoint ValidatePosition(OxyRect plotArea, ScreenPoint position)
            {
                double positionX = position.X;

                if (positionX < plotArea.Left)
                    positionX = plotArea.Left;
                else if (positionX > plotArea.Right)
                    positionX = plotArea.Right;

                position = new ScreenPoint(positionX, position.Y);
                return position;
            }

            private OxyRect GetZoomOxyRect(ScreenPoint position, OxyRect plotArea)
            {                
                var zoomOxyRect = new OxyRect(
                    Math.Min(position.X, StartPosition.X),
                    Math.Min(position.Y, StartPosition.Y),
                    Math.Abs(position.X - StartPosition.X),
                    Math.Abs(position.Y - StartPosition.Y));
                return zoomOxyRect;
            }

            #endregion

            #region private fields

            private readonly GenericTrendsPlotView _trendsPlotView;

            #endregion
        }        
    }
}