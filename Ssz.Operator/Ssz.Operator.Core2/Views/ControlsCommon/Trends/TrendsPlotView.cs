using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.ControlsCommon.Trends.Converters;
using OxyPlot.Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using OxyPlot;

namespace Ssz.Operator.Core.ControlsCommon.Trends
{
    [TemplatePart(Name = Plot_PART, Type = typeof(PlotView))]
    [TemplatePart(Name = XAxis_PART, Type = typeof(DateTimeAxis))]
    public abstract class TrendsPlotView : TemplatedControl
    {
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

        public PlotView? Plot { get; private set; }

        public DateTimeAxis? XAxis { get; private set; }

        #endregion

        #region protected functions

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            Plot = e.NameScope.Find(Plot_PART) as PlotView;
            XAxis = e.NameScope.Find(XAxis_PART) as DateTimeAxis;

            Bind(MinimumVisibleTimeProperty, new Binding("VisibleDateRange.Minimum") { Mode = BindingMode.OneWay });
            Bind(MaximumVisibleTimeProperty, new Binding("VisibleDateRange.Maximum") { Mode = BindingMode.OneWay });

            Bind(TrendItemsProperty, new Binding("Items"));
            Bind(SelectedTrendProperty, new Binding("SelectedItem"));

            if (XAxis != null)
            {
                XAxis.Bind(Axis.MinimumProperty, new Binding("VisibleDateRange.Minimum")
                {
                    Converter = new DateTimeToDoubleConverter()
                });

                XAxis.Bind(Axis.MaximumProperty, new Binding("VisibleDateRange.Maximum")
                {
                    Converter = new DateTimeToDoubleConverter()
                });
            }

            if (Plot != null)
            {
                try
                {
                    Plot.ApplyTemplate();
                    //FixBugWithNotRegisteringMouseClickedOnPlotArea();
                }
                catch
                {
                }
            }

            RefreshLines();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == MinimumVisibleTimeProperty ||
                e.Property == MaximumVisibleTimeProperty ||
                e.Property == BoundsProperty)
            {
                int verticalLinesCount;
                if (!Double.IsNaN(Bounds.Width))
                    verticalLinesCount = (int)(Bounds.Width / 80);
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
                .ForEach(item => AddLine(item));
        }

        protected virtual IEnumerable<TrendViewModel> TrendItemsInDisplayOrder(IEnumerable<TrendViewModel> items)
        {
            var trendItemsList = items.ToList();
            IEnumerable<TrendViewModel> trendItemsInDisplayOrder;
            if (SelectedTrendItem is not null)
                trendItemsInDisplayOrder = trendItemsList.Contains(SelectedTrendItem)
                    ? trendItemsList.Except(new[] { SelectedTrendItem }).Concat(new[] { SelectedTrendItem })
                    : trendItemsList;
            else
                trendItemsInDisplayOrder = trendItemsList;
            return trendItemsInDisplayOrder;
        }

        protected virtual void ClearLines()
        {
            if (Plot != null)
            {
                Plot.Model.Series.Clear();
            }
        }

        protected virtual LineSeries AddLine(TrendViewModel trendItem)
        {
            var lineSeries = new LineSeries
            {
                DataContext = trendItem,
                StrokeThickness = trendItem == SelectedTrendItem ? 3 : 1,
                Mapping = obj =>
                {
                    var point = (TrendPoint)obj;
                    return new DataPoint(
                        new DateTimeOffset(point.Timestamp).ToUnixTimeMilliseconds(),
                        point.Value
                    );
                }
            };
            
            lineSeries.Bind(
                ItemsControl.ItemsSourceProperty,
                new Binding("Points"));
            
            lineSeries.Bind(
                LineSeries.IsVisibleProperty,
                new Binding("IsDisplayedOnPlot"));

            lineSeries.Bind(
                Series.ColorProperty,
                new Binding("Color"));

            //if (Plot != null)
            //{
            //    Plot.Model.Series.Add(lineSeries);
            //}

            return lineSeries;
        }

        protected IEnumerable<TrendViewModel> TrendItems
        {
            get { return (IEnumerable<TrendViewModel>)GetValue(TrendItemsProperty)!; }
        }

        #endregion

        #region private functions

        protected TrendViewModel? SelectedTrendItem
        {
            get { return (TrendViewModel?)GetValue(SelectedTrendProperty); }
        }

        private void OnGlobalUITimerEvent(int phase)
        {
            if (phase != 0) return;

            var viewModel = DataContext as TrendsViewModel;
            if (RefreshOnTimerTick && viewModel != null)
                viewModel.OnTimerTick();
        }

        private DateTime MinimumVisibleTime
        {
            get { return (DateTime)GetValue(MinimumVisibleTimeProperty)!; }
        }

        private DateTime MaximumVisibleTime
        {
            get { return (DateTime)GetValue(MaximumVisibleTimeProperty)!; }
        }

        //// http://stackoverflow.com/questions/29565113/oxyplots-mouse-events-are-not-caught-while-model-invalidated
        //// Of couse, this is oxyplot version specific and might break in the future. Updating oxyplot might be an option (if it doesnt break other stuff).
        //// The goal of this is to prevent missed mouse clickes sometimes, when they are received by detached elements inside plot for whatever reason.
        //private void FixBugWithNotRegisteringMouseClickedOnPlotArea()
        //{
        //    var mouseGrid = new Grid
        //    {
        //        Background = Brushes.Transparent // background must be set for hit test to work
        //    };

        //    var grid = (Grid)((FieldInfo)
        //        typeof(PlotView).GetMember("grid", BindingFlags.NonPublic | BindingFlags.Instance)[0])
        //        .GetValue(Plot)!;
        //    grid.Children.Add(mouseGrid);
        //}

        #endregion

        #region private fields

        protected static readonly AvaloniaProperty MinimumVisibleTimeProperty = AvaloniaProperty.Register<TrendsPlotView, DateTime>("MinimumVisibleTime", DateTime.MinValue);

        protected static readonly AvaloniaProperty MaximumVisibleTimeProperty = AvaloniaProperty.Register<TrendsPlotView, DateTime>("MaximumVisibleTime", DateTime.MaxValue);

        private static readonly AvaloniaProperty TrendItemsProperty = AvaloniaProperty.Register<TrendsPlotView, IEnumerable<TrendViewModel>>("TrendItems");

        protected static readonly AvaloniaProperty SelectedTrendProperty = AvaloniaProperty.Register<TrendsPlotView, TrendViewModel>("SelectedTrend");

        #endregion
    }
}
