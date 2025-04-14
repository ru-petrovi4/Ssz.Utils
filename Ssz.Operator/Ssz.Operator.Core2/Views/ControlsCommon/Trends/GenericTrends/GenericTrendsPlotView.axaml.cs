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

            configuration.PlotAreaBackgroundColor = ((SolidColorBrush) Plot.PlotAreaBackground).Color;
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
            if (YAxis != null)
            {
                YAxis.Bind(Axis.AbsoluteMinimumProperty, new Binding("SelectedItem.YMinWithPadding") { TargetNullValue = 0.0 });
                YAxis.Bind(Axis.AbsoluteMaximumProperty, new Binding("SelectedItem.YMaxWithPadding") { TargetNullValue = 100.0 });
                YAxis.Bind(Axis.MinimumProperty, new Binding("SelectedItem.AxisMinimumWithPadding") { TargetNullValue = 0.0 });
                YAxis.Bind(Axis.MaximumProperty, new Binding("SelectedItem.AxisMaximumWithPadding") { TargetNullValue = 100.0 });
                YAxis.Bind(Axis.MajorStepProperty, new Binding("SelectedItem.MajorStep") { TargetNullValue = 5.0 });
                YAxis.Bind(Axis.MinorStepProperty, new Binding("SelectedItem.MinorStep") { TargetNullValue = 10.0 });

                YAxis.Bind(Axis.StringFormatProperty, new Binding("SelectedItem.ValueFormat") { TargetNullValue = "F02" });

                Bind(SelectedItemColorProperty, new Binding("SelectedItem.Color") { TargetNullValue = Colors.Black });
            }
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
                    MinimumPadding = 10,
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

            if (e.Property == SelectedItemProperty)
            {
                if (Plot is null)
                    return;                          

                Plot.Series.Clear();
                foreach (LineSeries seria in Plot.Series.ToArray())
                {
                    if (seria.DataContext == SelectedItem)
                    {
                        Plot.Series.Remove(seria);
                        seria.StrokeThickness = 3;
                        Plot.Series.Add(seria);
                    }
                    else
                    {
                        seria.StrokeThickness = 1;
                        
                    }
                }
                // TODO Reorder.
                //Plot.Series.Add(seria);
            }

            if (e.Property == MinimumVisibleTimeProperty ||
                e.Property == MaximumVisibleTimeProperty)
            {
                UpdateValueLabels();
            }

            if (e.Property == SelectedItemColorProperty && YAxis is not null)
            {
                YAxis.TitleColor = SelectedItemColor;
                YAxis.InternalAxis.TextColor = SelectedItemColor.ToOxyColor();
                YAxis.TicklineColor = SelectedItemColor;
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