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

        public static readonly AvaloniaProperty SelectedTrendColorProperty = AvaloniaProperty.Register<GenericTrendsPlotView, Color>(
            nameof(SelectedTrendColor), Colors.Red);

        public Color SelectedTrendColor
        {
            get { return (Color)GetValue(SelectedTrendColorProperty)!; }
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
            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedTrend);
            if (axis == null)
                return;

            axis.ZoomIn();
        }

        public void ZoomSelectedValueAxisOut()
        {
            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedTrend);
            if (axis == null)
                return;

            axis.ZoomOut();
        }

        public void GetSelectedValueAxisMinimumAndMaximum(out double minimum, out double maximum)
        {
            minimum = 0;
            maximum = 100;

            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedTrend);
            if (axis == null)
                return;

            minimum = axis.InternalAxis.ActualMinimum;
            maximum = axis.InternalAxis.ActualMaximum;
        }

        public void SetSelectedValueAxisMinimumAndMaximum(double minimum, double maximum)
        {
            var axis = (GenericYAxis?) Plot.Axes.FirstOrDefault(ax => ax.DataContext == SelectedTrend);
            if (axis == null)
                return;

            axis.SetMinimumAndMaximum(minimum, maximum);
        }

        public void CenterPlotAroundDisplayValueSliderPosition()
        {
            var viewModel = (GenericTrendsViewModel) DataContext;

            viewModel.CenterMinAndMaxAroundCurrentDisplayedValue();
        }

        public void WriteSettings(TrendsGroupConfiguration configuration)
        {
            configuration.PlotAreaBackgroundColor = ((SolidColorBrush) Plot!.PlotAreaBackground).Color;
            //configuration.PlotBackgroundColor = ((SolidColorBrush) Plot.Background).Color;

            configuration.DsTrendItemsCollection = Plot.Axes.Skip(2).Select(a => ((TrendViewModel)a.DataContext).Source.DsTrendItem).ToArray();
        }

        public void ReadSettings(TrendsGroupConfiguration configuration)
        {
            if (configuration.PlotAreaBackgroundColor != null)
                Plot.PlotAreaBackground = new SolidColorBrush(configuration.PlotAreaBackgroundColor.Value);
            if (configuration.PlotBackgroundColor != null)
                Plot.Background = new SolidColorBrush(configuration.PlotBackgroundColor.Value);

            if (configuration.DsTrendItemsCollection != null)
            {
                var viewModel = (TrendsViewModel) DataContext;
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
                //Plot.ApplyTemplate();
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
            if (YAxis != null)
            {
                YAxis.SetBinding(Axis.AbsoluteMinimumProperty, new Binding("SelectedItem.YMinWithPadding"));
                YAxis.SetBinding(Axis.AbsoluteMaximumProperty, new Binding("SelectedItem.YMaxWithPadding"));
                YAxis.SetBinding(Axis.MinimumProperty, new Binding("SelectedItem.AxisMinimumWithPadding"));
                YAxis.SetBinding(Axis.MaximumProperty, new Binding("SelectedItem.AxisMaximumWithPadding"));
                YAxis.SetBinding(Axis.MajorStepProperty, new Binding("SelectedItem.MajorStep"));
                YAxis.SetBinding(Axis.MinorStepProperty, new Binding("SelectedItem.MinorStep"));

                YAxis.SetBinding(Axis.StringFormatProperty, new Binding("SelectedItem.ValueFormat"));

                SetBinding(SelectedTrendColorProperty, new Binding("SelectedItem.Color"));
            }
        }

        protected override void RefreshLines()
        {
            if (Plot == null)
                return;

            if (TrendItems == null)
                return;

            List<TrendViewModel> trendItems = TrendItems.ToList();

            ClearLines();

            int nItem = 0;
            foreach (TrendViewModel trendItem in trendItems)
            {
                var axis = new GenericYAxis
                {
                    PositionTier = Plot.Axes.Count - 2,
                    AxislineStyle = LineStyle.Solid,
                    MinimumPadding = 10,
                    IsZoomEnabled = false,
                    DataContext = trendItem,
                    Key = nItem.ToString(CultureInfo.InvariantCulture)
                };
                axis.Position = AxisPosition.Left;                

                Plot.Axes.Add(axis);
                nItem ++;
            }

            foreach (TrendViewModel trendItem in TrendItemsInDisplayOrder(trendItems))
            {
                LineSeries series = AddLine(trendItem);
                series.YAxisKey = trendItems.IndexOf(trendItem).ToString(CultureInfo.InvariantCulture);
            }            
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

            if (e.Property == SelectedTrendProperty)
            {
                if (Plot == null)
                    return;

                var value = GetValue(SelectedTrendProperty) as TrendViewModel;
                if (value == null)
                    return;

                List<LineSeries> series = Plot.Series
                    .OfType<LineSeries>()
                    .OrderBy(s => s.DataContext == SelectedTrend).ToList();

                Plot.Series.Clear();
                foreach (LineSeries seria in series)
                {
                    Plot.Series.Add(seria);

                    // selected line always goes last
                    seria.StrokeThickness = Equals(seria, series.Last())
                        ? 3
                        : 1;
                }                
            }

            if (e.Property == MinimumVisibleTimeProperty ||
                e.Property == MaximumVisibleTimeProperty)
            {
                UpdateValueLabels();
            }

            if (e.Property == SelectedTrendColorProperty)
            {
                YAxis.TitleColor = SelectedTrendColor;
                YAxis.InternalAxis.TextColor = SelectedTrendColor.ToOxyColor();
                YAxis.TicklineColor = SelectedTrendColor;
            }
        }

        #endregion

        #region private functions

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
            if (AdditionalGrid == null)
                return;

            var viewModel = DataContext as TrendsViewModel;
            if (viewModel == null)
                return;

            foreach (var child in AdditionalGrid.Children)
            {
                UpdateRulerValues(child as Slider);
            }
        }

        private void UpdateRulerValues(Slider? ruler)
        {
            if (ruler == null)
                return;

            var viewModel = DataContext as TrendsViewModel;
            if (viewModel == null)
                return;

            var valuesControl = TreeHelper.FindChild<ValuesControl>(ruler, "ValuesControl");
            if (valuesControl == null)
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
                    if (value == null || Double.IsNaN(value.Value)) continue;
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
            if (AdditionalGrid == null)
                return;

            if (Plot.ActualModel != null)
            {
                Canvas.SetLeft(AdditionalGrid, Plot.ActualModel.PlotArea.Left);
                Canvas.SetTop(AdditionalGrid, Plot.ActualModel.PlotArea.Top);
                AdditionalGrid.Width = Plot.ActualModel.PlotArea.Width;
                AdditionalGrid.Height = Plot.ActualModel.PlotArea.Height;
            }
        }

        private void TryAddRuler(ScreenPoint position)
        {
            if (AdditionalGrid == null)
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
            if (AdditionalGrid == null)
                return;

            foreach (var ruler in AdditionalGrid.Children.OfType<Slider>().ToArray())
            {
                AdditionalGrid.Children.Remove(ruler);
            }    
            //Plot!.HideZoomRectangle();

            //if (_completedZoomRect_DataPoint0 == null || _completedZoomRect_DataPoint1 == null)
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

        #endregion

        private class GenericYAxis : LinearAxis
        {
            #region construction and destruction

            public GenericYAxis()
            {
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

                if ((e.Property == YMinProperty || e.Property == YMaxProperty) &&
                    YMin < YMax)
                {
                    if (!_minMaxOverriden)
                    {
                        Minimum = YMin - (YMax - YMin) * YAxisCoefficient;
                        Maximum = YMax + (YMax - YMin) * YAxisCoefficient;
                    }
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
            private const double YAxisCoefficient = 0.1; // 10%            

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
                _trendsPlotView.Plot.ZoomRectangleTemplate = (ControlTemplate)_trendsPlotView.Resources["ZoomRectangleTemplate"];

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

                _trendsPlotView.Plot.ZoomRectangleTemplate = (ControlTemplate)_trendsPlotView.Resources["CompletedZoomRectangleTemplate"];

                OxyRect plotArea = PlotView.ActualModel.PlotArea;
                ScreenPoint position = ValidatePosition(plotArea, args.Position);
                OxyRect zoomOxyRect = GetZoomOxyRect(position, plotArea);
                PlotView.ShowZoomRectangle(zoomOxyRect);

                _trendsPlotView.Plot.ReleaseMouseCapture();

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