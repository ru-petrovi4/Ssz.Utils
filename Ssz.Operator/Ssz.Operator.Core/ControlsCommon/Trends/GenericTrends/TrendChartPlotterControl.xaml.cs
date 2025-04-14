using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    public partial class TrendChartPlotterControl : UserControl
    {
        #region construction and destruction

        public TrendChartPlotterControl()
        {
            InitializeComponent();

            ChartPlotter.Children.Add(new RemoveAll());
            ZoomRestriction.HorizontalScrollBar = HorizontalScrollBar;
            ZoomRestriction.VerticalScrollBar = VerticalScrollBar;

            HorizontalScrollBar.ValueChanged += HorizontalScrollBarValueChanged;
            VerticalScrollBar.ValueChanged += VerticalScrollBarValueChanged;

            ChartPlotter.HorizontalAxis = _horizontalAxis;
            ChartPlotter.VerticalAxis = _verticalAxis;
            ChartPlotter.ContextMenu.Visibility = Visibility.Hidden;

            _valueRange.Fill = _alarmRange.Fill = _blockRange.Fill = Brushes.Transparent;
            _alarmRange.Stroke = Brushes.Yellow;
            _valueRange.StrokeDashArray =
                _alarmRange.StrokeDashArray = _blockRange.StrokeDashArray = new DoubleCollection(new double[] {5, 5});
            _valueRange.StrokeThickness = _alarmRange.StrokeThickness = _blockRange.StrokeThickness = 2;
            _blockRange.Stroke = Brushes.Red;
            _valueRange.Opacity = _alarmRange.Opacity = _blockRange.Opacity = 1;

            _verticalAxis.ShowMinorTicks = false;
            _verticalAxis.ClipToBounds = _verticalAxis.AxisControl.ClipToBounds = false;
            _horizontalAxis.LabelProvider.SetCustomFormatter(info => info.Tick.ToString("T"));
            _horizontalAxis.AxisControl.MayorLabelProvider = null;


            ChartPlotter.Viewport.Restrictions.Add(ZoomRestriction);
            ChartPlotter.LegendVisible = false;

            ChartPlotter.BottomPanel.Children.Insert(0, _horizontalLine);
            ChartPlotter.LeftPanel.Children.Add(_verticalLine);
            Path path = ChartPlotter.AxisGrid.GridPath;
            path.Opacity = 1;
            path.StrokeThickness = 1;

            ChartPlotterBackground = Brushes.Black;
            ChartPlotterGridBrush = Brushes.DimGray;
            AxisBrush = Brushes.White;

            Update();

            HorizontalScrollBar.Value = HorizontalScrollBar.Maximum;
            ChartPlotter.Children.Add(_alarmRange);
            ChartPlotter.Children.Add(_blockRange);
            ChartPlotter.Children.Add(_valueRange);
            ChartPlotter.ForceCursor = false;

            DsProject.Instance.GlobalUITimerEvent +=
                OnGlobalUITimerEvent;

            Unloaded += TrendControlUnloaded;
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
            typeof(object),
            typeof(
                TrendChartPlotterControl),
            new PropertyMetadata
                (SelectedItemChanged));

        public static readonly DependencyProperty AxeBrushProperty = DependencyProperty.Register("AxeBrush",
            typeof(Brush),
            typeof(TrendChartPlotterControl));

        public static readonly DependencyProperty AxeForegroundProperty = DependencyProperty.Register("AxeForeground",
            typeof(Brush),
            typeof(
                TrendChartPlotterControl));

        public readonly HWZoomRestriction ZoomRestriction = new();

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public Brush? ChartPlotterBackground
        {
            get => ChartPlotter.Background;
            set => ChartPlotter.Background = value;
        }

        public Brush? ChartPlotterGridBrush
        {
            get => ChartPlotter.AxisGrid.GridPath.Stroke;
            set => ChartPlotter.AxisGrid.GridPath.Stroke = value;
        }

        public Brush? AxisBrush
        {
            set =>
                _valueRange.Stroke = _horizontalAxis.AxisControl.Foreground =
                    _horizontalLine.Stroke = _horizontalAxis.AxisControl.TicksPath.Stroke = value;
        }

        public ObservableCollection<Trend>? TrendItemViewsCollection
        {
            get => _trendItemViewsCollection;
            set
            {
                if (_trendItemViewsCollection == value)
                    return;
                if (value is null)
                    return;
                if (_trendItemViewsCollection is not null)
                    _trendItemViewsCollection.CollectionChanged -= TrendItemViewsCollectionCollectionChanged;
                _trendItemViewsCollection = value;
                _trendItemViewsCollection.CollectionChanged += TrendItemViewsCollectionCollectionChanged;
                AddSeries(_trendItemViewsCollection);
            }
        }

        public void Update()
        {
            IEnumerable<ObservableCollection<KeyValuePair<DateTime, double>>> dataSourcesCollections = ChartPlotter
                .Children.OfType<LineGraph>().Select(
                    g => (ObservableDataSource<KeyValuePair<DateTime, double>>) g.DataSource)
                .Select(s => s.Collection);

            var isParked = HorizontalScrollBar.Value >
                           HorizontalScrollBar.Maximum +
                           (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum) * 0.01;
            DateTime[] firstList = dataSourcesCollections.Where(g => g.Count > 0).Select(c => c[0].Key).ToArray();
            DateTime[] lastList =
                dataSourcesCollections.Where(g => g.Count > 0).Select(c => c[c.Count - 1].Key).ToArray();
            if (firstList.Length == 0 || lastList.Length == 0)
                return;
            var timeMin = firstList.Min();
            var timeMax = lastList.Max();
            //if (timeMin == _timeMin && timeMax == _timeMax)
            //    return;

            var horizontalScrollBarValueRelative = (HorizontalScrollBar.Value - HorizontalScrollBar.Minimum) /
                                                   (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);
            HorizontalScrollBar.Minimum = 0;
            HorizontalScrollBar.Maximum = (timeMax - timeMin - ZoomRestriction.Interval).Ticks +
                                          new TimeSpan(0, 0, 2).Ticks;
            HorizontalScrollBar.SmallChange =
                HorizontalScrollBar.LargeChange = (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum) / 20;

            if (isParked)
                HorizontalScrollBar.Value = HorizontalScrollBar.Maximum;
            else if (!double.IsNaN(horizontalScrollBarValueRelative))
                HorizontalScrollBar.Value = HorizontalScrollBar.Minimum +
                                            horizontalScrollBarValueRelative *
                                            (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum);
            else HorizontalScrollBar.Value = HorizontalScrollBar.Maximum;
            ZoomRestriction.TimeMin = timeMin;
            _timeMin = timeMin;
            _timeMax = timeMax;

            ChartPlotter.Viewport.FitToView();
        }

        #endregion

        #region private functions

        private static void SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TrendChartPlotterControl;
            var trendItemView = e.NewValue as Trend;

            if (control is not null && e.OldValue is not null)
            {
                var oldSeria = (Trend) e.OldValue;
                oldSeria.PropertyChanged -= control.TrendItemViewOnChanged;
                var oldPlot =
                    control.ChartPlotter.Children.OfType<LineGraph>()
                        .FirstOrDefault(l => l.DataSource == oldSeria.ObservableDataSource);
                if (oldPlot is not null)
                    oldPlot.ZIndex = 0;
            }

            if (control is null) return;

            control.SetCurrentItemProps(trendItemView);

            if (trendItemView is null) return;

            trendItemView.PropertyChanged += control.TrendItemViewOnChanged;

            var plot =
                control.ChartPlotter.Children.OfType<LineGraph>()
                    .FirstOrDefault(l => l.DataSource == trendItemView.ObservableDataSource);
            if (plot is not null)
                plot.ZIndex = int.MaxValue;
        }

        private void OnGlobalUITimerEvent(int phase)
        {
            if (phase == 0) Update();
        }

        private void TrendControlUnloaded(object? sender, RoutedEventArgs e)
        {
            if (SelectedItem is not null)
            {
                var trendItemView = SelectedItem as Trend;
                if (trendItemView is not null)
                    trendItemView.PropertyChanged -= TrendItemViewOnChanged;
            }

            if (_trendItemViewsCollection is not null)
                _trendItemViewsCollection.CollectionChanged -= TrendItemViewsCollectionCollectionChanged;

            DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;
        }

        private void TrendItemViewOnChanged(DependencyPropertyChangedEventArgs args)
        {
            if (SelectedItem is not null)
                SetCurrentItemProps(SelectedItem as Trend);
        }

        private void SetCurrentItemProps(Trend? trendItemView)
        {
            if (trendItemView is null) return;
            _verticalAxis.ConvertFromDouble =
                val => trendItemView.VisibleYMin + val * (trendItemView.VisibleYMax - trendItemView.VisibleYMin);
            _verticalAxis.ConvertToDouble =
                val => (val - trendItemView.VisibleYMin) / (trendItemView.VisibleYMax - trendItemView.VisibleYMin);
            _verticalAxis.AxisControl.Foreground = trendItemView.Brush;

            BindingOperations.ClearBinding(_verticalAxis.AxisControl.TicksPath, Shape.StrokeProperty);
            _verticalAxis.AxisControl.TicksPath.SetBinding(Shape.StrokeProperty,
                new Binding
                {
                    Source = trendItemView,
                    Path = new PropertyPath("Brush"),
                    Mode = BindingMode.OneWay
                });
            BindingOperations.ClearBinding(_verticalAxis.AxisControl, ForegroundProperty);
            _verticalAxis.AxisControl.SetBinding(ForegroundProperty,
                new Binding
                {
                    Source = trendItemView,
                    Path = new PropertyPath("Brush"),
                    Mode = BindingMode.OneWay
                });
            BindingOperations.ClearBinding(_verticalLine, Shape.StrokeProperty);
            _verticalLine.SetBinding(Shape.StrokeProperty, new Binding
            {
                Source = trendItemView,
                Path = new PropertyPath("Brush"),
                Mode = BindingMode.OneWay
            });
            Func<KeyValuePair<DateTime, double>, double> ymapping = trendItemView.YMapping;
            if (!double.IsNaN(trendItemView.LoAlarmLimit) &&
                trendItemView.LoAlarmLimit > trendItemView.YMin &&
                trendItemView.LoAlarmLimit < trendItemView.YMax)
                _alarmRange.Value1 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.LoAlarmLimit));
            else
                _alarmRange.Value1 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.YMin));
            if (!double.IsNaN(trendItemView.HiAlarmLimit) &&
                trendItemView.HiAlarmLimit > trendItemView.YMin &&
                trendItemView.HiAlarmLimit < trendItemView.YMax)
                _alarmRange.Value2 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.HiAlarmLimit));
            else
                _alarmRange.Value2 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.YMax));
            if (!double.IsNaN(trendItemView.LoLoAlarmLimit) &&
                trendItemView.LoLoAlarmLimit > trendItemView.YMin &&
                trendItemView.LoLoAlarmLimit < trendItemView.YMax)
                _blockRange.Value1 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.LoLoAlarmLimit));
            else
                _blockRange.Value1 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.YMin));
            if (!double.IsNaN(trendItemView.HiHiAlarmLimit) &&
                trendItemView.HiHiAlarmLimit > trendItemView.YMin &&
                trendItemView.HiHiAlarmLimit < trendItemView.YMax)
                _blockRange.Value2 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.HiHiAlarmLimit));
            else
                _blockRange.Value2 =
                    ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.YMax));

            _valueRange.Value1 = ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.YMin));
            _valueRange.Value2 = ymapping(new KeyValuePair<DateTime, double>(new DateTime(), trendItemView.YMax));
            ChartPlotter.Viewport.FitToView();
        }

        private void HorizontalScrollBarValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChartPlotter.Viewport.FitToView();
        }

        private void VerticalScrollBarValueChanged(object? sender, RoutedPropertyChangedEventArgs<double> e)
        {
            ChartPlotter.Viewport.FitToView();
        }

        private void AddSeries(IEnumerable<Trend> trendItemViews)
        {
            foreach (Trend trendItemView in trendItemViews.ToArray())
            {
                if (trendItemView.ObservableDataSource is null) continue;
                trendItemView.ObservableDataSource.SetXMapping(dp => _horizontalAxis.ConvertToDouble(dp.Key));
                trendItemView.ObservableDataSource.SetYMapping(trendItemView.YMapping);
                LineGraph lineGraph = ChartPlotter.AddLineGraph(trendItemView.ObservableDataSource,
                    Colors.White, trendItemView.Thicknes);
                lineGraph.SetBinding(LineGraph.LinePenProperty,
                    new Binding
                    {
                        Source = trendItemView,
                        Path = new PropertyPath("Brush"),
                        Converter = new BrushToPenConverter(),
                        ConverterParameter = trendItemView
                    });
                lineGraph.SetBinding(VisibilityProperty,
                    new Binding
                    {
                        Source = trendItemView,
                        Path = new PropertyPath("Visible"),
                        Converter = new BooleanToVisibilityConverter()
                    });
            }

            ChartPlotter.LegendVisible = false;
        }

        private void TrendItemViewsCollectionCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
                AddSeries((e.NewItems ?? throw new InvalidOperationException()).OfType<Trend>());
            if (e.Action == NotifyCollectionChangedAction.Remove)
                foreach (LineGraph lineGraph in (e.OldItems ?? throw new InvalidOperationException())
                    .OfType<Trend>()
                    .SelectMany(trend1 => ChartPlotter.Children.OfType<LineGraph>()
                        .Where(g => g.DataSource == trend1.ObservableDataSource).ToArray()))
                    ChartPlotter.Children.Remove(lineGraph);
            if (e.Action == NotifyCollectionChangedAction.Reset)
                foreach (LineGraph lineGraph in ChartPlotter.Children.OfType<LineGraph>().ToArray())
                    ChartPlotter.Children.Remove(lineGraph);
            ChartPlotter.LegendVisible = false;
            Update();
        }

        private void ChartPlotterOnMouseWheel(object? sender, MouseWheelEventArgs e)
        {
            HorizontalScrollBar.Value -=
                (HorizontalScrollBar.Maximum - HorizontalScrollBar.Minimum) * 0.01 * e.Delta / 120.0;

            /*
            double intervalSecondsMin = TimeSpan.FromSeconds(10).TotalSeconds;
            double intervalSecondsMax = TimeSpan.FromDays(2).TotalSeconds;
            double intervalSeconds = ZoomRestriction.Interval.TotalSeconds;

            if (e.Delta < 0)
                intervalSeconds = intervalSeconds * (-(double)e.Delta / 60);
            else
                intervalSeconds = intervalSeconds / ((double)e.Delta / 60);
            if (intervalSeconds < intervalSecondsMin)
                intervalSeconds = intervalSecondsMin;
            if (intervalSeconds > intervalSecondsMax)
                intervalSeconds = intervalSecondsMax;
            ZoomRestriction.Interval = TimeSpan.FromSeconds(intervalSeconds);
            Update();*/
        }

        #endregion

        #region private fields

        private readonly HorizontalDateTimeAxis _horizontalAxis = new();
        private readonly VerticalAxis _verticalAxis = new();
        private readonly HorizontalRange _valueRange = new();
        private readonly HorizontalRange _alarmRange = new();
        private readonly HorizontalRange _blockRange = new();

        private readonly Line _horizontalLine = new()
        {
            StrokeThickness = 2,
            X1 = 0,
            X2 = 100,
            Y1 = 0,
            Y2 = 0,
            Stretch = Stretch.Fill
        };

        private readonly Line _verticalLine = new()
        {
            StrokeThickness = 2,
            X1 = 0,
            X2 = 0,
            Y1 = 0,
            Y2 = 100,
            Stretch = Stretch.Fill
        };

        private ObservableCollection<Trend>? _trendItemViewsCollection =
            new();

        private DateTime _timeMin, _timeMax;

        #endregion
    }
}