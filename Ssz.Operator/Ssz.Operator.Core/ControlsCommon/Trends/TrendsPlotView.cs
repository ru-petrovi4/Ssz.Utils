using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.ControlsCommon.Trends.Converters;
using OxyPlot;
using OxyPlot.Wpf;
using DateTimeAxis = OxyPlot.Axes.DateTimeAxis;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    [TemplatePart(Name = Plot_PART, Type = typeof(PlotView))]
    [TemplatePart(Name = XAxis_PART, Type = typeof(DateTimeAxis))]
    public abstract class TrendsPlotView : Control
    {
        #region construction and destruction

        static TrendsPlotView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TrendsPlotView),
                new FrameworkPropertyMetadata(typeof(TrendsPlotView)));
        }

        #endregion

        #region public functions

        public const string Plot_PART = "Plot";
        public const string XAxis_PART = "XAxis";

        protected TrendsPlotView()
        {
            Loaded += (s, e) =>
            {
                DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;
            };

            Unloaded += (s, e) =>
            {
                DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;
            };
        }

        public bool RefreshOnTimerTick = true;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            Plot = (PlotView)GetTemplateChild(Plot_PART);
            XAxis = (OxyPlot.Wpf.DateTimeAxis)GetTemplateChild(XAxis_PART);

            SetBinding(MinimumVisibleTimeProperty, new Binding("VisibleDateRange.Minimum") { Mode = BindingMode.OneWay });
            SetBinding(MaximumVisibleTimeProperty, new Binding("VisibleDateRange.Maximum") { Mode = BindingMode.OneWay });

            SetBinding(TrendItemsProperty, new Binding("Items"));
            SetBinding(SelectedTrendProperty, new Binding("SelectedItem"));

            if (XAxis != null)
            {
                XAxis.SetBinding(Axis.MinimumProperty, new Binding("VisibleDateRange.Minimum")
                {
                    Converter = new DateTimeToDoubleConverter()
                });

                XAxis.SetBinding(Axis.MaximumProperty, new Binding("VisibleDateRange.Maximum")
                {
                    Converter = new DateTimeToDoubleConverter()
                });
            }

            if (Plot != null)
            {
                try
                {
                    Plot.ApplyTemplate();
                    FixBugWithNotRegisteringMouseClickedOnPlotArea();
                }
                catch
                {
                }
            }

            RefreshLines();
        }

        public PlotView? Plot { get; private set; } = null;

        public OxyPlot.Wpf.DateTimeAxis? XAxis { get; private set; }

        #endregion

        #region protected functions

        protected static readonly DependencyProperty MinimumVisibleTimeProperty = DependencyProperty.Register("MinimumVisibleTime",
            typeof(DateTime),
            typeof(TrendsPlotView),
            new PropertyMetadata(DateTime.MinValue));

        protected static readonly DependencyProperty MaximumVisibleTimeProperty = DependencyProperty.Register("MaximumVisibleTime",
            typeof(DateTime),
            typeof(TrendsPlotView),
            new PropertyMetadata(DateTime.MaxValue));

        protected static readonly DependencyProperty TrendItemsProperty = DependencyProperty.Register(
            "TrendItems",
            typeof(IEnumerable<TrendViewModel>),
            typeof(TrendsPlotView),
            new PropertyMetadata(null));

        protected static readonly DependencyProperty SelectedTrendProperty = DependencyProperty.Register(
            "SelectedTrend",
            typeof(TrendViewModel),
            typeof(TrendsPlotView),
            new PropertyMetadata(null));

        private DateTime MinimumVisibleTime
        {
            get { return (DateTime)GetValue(MinimumVisibleTimeProperty); }
        }

        private DateTime MaximumVisibleTime
        {
            get { return (DateTime)GetValue(MaximumVisibleTimeProperty); }
        }

        protected IEnumerable<TrendViewModel> TrendItems
        {
            get { return (IEnumerable<TrendViewModel>)GetValue(TrendItemsProperty); }
        }

        protected TrendViewModel? SelectedTrend
        {
            get { return (TrendViewModel?)GetValue(SelectedTrendProperty); }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == MinimumVisibleTimeProperty ||
                e.Property == MaximumVisibleTimeProperty ||
                e.Property == ActualWidthProperty)
            {
                int verticalLinesCount;
                if (!Double.IsNaN(ActualWidth))
                    verticalLinesCount = (int)(ActualWidth / 80);
                else
                    verticalLinesCount = 5;
                var visibleTime = MaximumVisibleTime - MinimumVisibleTime;
                var minimumStep = TimeSpan.FromSeconds(15);
                while (visibleTime.Ticks/minimumStep.Ticks > verticalLinesCount)
                    minimumStep = TimeSpan.FromSeconds(minimumStep.TotalSeconds*2);

                if (XAxis != null)
                {
                    XAxis.MajorStep = minimumStep.TotalDays;
                }
            }

            if (e.Property == TrendItemsProperty)
            {
                RefreshLines();
            }
        }

        protected virtual void RefreshLines()
        {
            var trendItems = TrendItems;
            if (trendItems == null)
                return;

            ClearLines();

            var trendItemsInDisplayOrder = TrendItemsInDisplayOrder(trendItems);

            trendItemsInDisplayOrder
                .ToList()
                .ForEach(item => AddLine(item))
                ;
        }

        protected virtual IEnumerable<TrendViewModel> TrendItemsInDisplayOrder(IEnumerable<TrendViewModel> items)
        {
            var trendItemsList = items.ToList();
            IEnumerable<TrendViewModel> trendItemsInDisplayOrder;
            if (SelectedTrend is not null)
                trendItemsInDisplayOrder = trendItemsList.Contains(SelectedTrend)
                    ? trendItemsList.Except(new[] { SelectedTrend }).Concat(new[] { SelectedTrend })
                    : trendItemsList;
            else
                trendItemsInDisplayOrder = trendItemsList;
            return trendItemsInDisplayOrder;
        }

        protected virtual void ClearLines()
        {
            if (Plot != null)
            {
                Plot.Series.Clear();
            }
        }

        protected virtual LineSeries AddLine(TrendViewModel trendItem)
        {
            var lineSeries = new LineSeries
            {
                DataContext = trendItem,
                StrokeThickness = trendItem == SelectedTrend ? 3 : 1,
                Mapping = obj =>
                {
                    var point = (TrendPoint)obj;
                    return new DataPoint
                    {
                        X = DateTimeAxis.ToDouble(point.Timestamp),
                        Y = point.Value
                    };
                }
            };
            
            lineSeries.SetBinding(
                ItemsControl.ItemsSourceProperty,
                new Binding("Points"));

            lineSeries.SetBinding(
                VisibilityProperty,
                new Binding("IsDisplayedOnPlot") { Converter = new BooleanToVisibilityConverter() });

            lineSeries.SetBinding(
                Series.ColorProperty,
                new Binding("Color"));

            if (Plot != null)
            {
                Plot.Series.Add(lineSeries);
            }

            return lineSeries;
        }        

        #endregion

        #region private functions

        private void OnGlobalUITimerEvent(int phase)
        {
            if (phase != 0) return;

            var viewModel = DataContext as TrendsViewModel;
            if (RefreshOnTimerTick && viewModel != null)
                viewModel.OnTimerTick();
        }        

        // http://stackoverflow.com/questions/29565113/oxyplots-mouse-events-are-not-caught-while-model-invalidated
        // Of couse, this is oxyplot version specific and might break in the future. Updating oxyplot might be an option (if it doesnt break other stuff).
        // The goal of this is to prevent missed mouse clickes sometimes, when they are received by detached elements inside plot for whatever reason.
        private void FixBugWithNotRegisteringMouseClickedOnPlotArea()
        {
            var mouseGrid = new Grid
            {
                Background = Brushes.Transparent // background must be set for hit test to work
            };

            var grid = (Grid)((FieldInfo)
                typeof(PlotView).GetMember("grid", BindingFlags.NonPublic | BindingFlags.Instance)[0])
                .GetValue(Plot)!;
            grid.Children.Add(mouseGrid);
        }

        #endregion        
    }
}
