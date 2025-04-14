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
    [TemplatePart(Name = Plot_PART, Type = typeof(Plot))]
    [TemplatePart(Name = XAxis_PART, Type = typeof(DateTimeAxis))]
    public abstract class TrendsPlotView : TemplatedControl
    {
        #region construction and destruction    

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

        #endregion

        #region public functions

        public const string Plot_PART = "Plot";
        public const string XAxis_PART = "XAxis";        

        public bool RefreshOnTimerTick = true;        

        public Plot? Plot { get; private set; }

        public DateTimeAxis? XAxis { get; private set; }

        #endregion

        #region protected functions

        protected static readonly AvaloniaProperty MinimumVisibleTimeProperty = AvaloniaProperty.Register<TrendsPlotView, DateTime>("MinimumVisibleTime", DateTime.MinValue);

        protected static readonly AvaloniaProperty MaximumVisibleTimeProperty = AvaloniaProperty.Register<TrendsPlotView, DateTime>("MaximumVisibleTime", DateTime.MaxValue);

        private static readonly AvaloniaProperty ItemsProperty = AvaloniaProperty.Register<TrendsPlotView, IEnumerable<TrendViewModel>>("Items");

        protected static readonly AvaloniaProperty SelectedItemProperty = AvaloniaProperty.Register<TrendsPlotView, TrendViewModel>("SelectedItem");

        protected DateTime MinimumVisibleTime
        {
            get { return (DateTime)GetValue(MinimumVisibleTimeProperty)!; }
        }

        protected DateTime MaximumVisibleTime
        {
            get { return (DateTime)GetValue(MaximumVisibleTimeProperty)!; }
        }

        protected IEnumerable<TrendViewModel> Items
        {
            get { return (IEnumerable<TrendViewModel>)GetValue(ItemsProperty)!; }
        }

        protected TrendViewModel? SelectedItem
        {
            get { return (TrendViewModel?)GetValue(SelectedItemProperty); }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            Plot = e.NameScope.Find(Plot_PART) as Plot;
            XAxis = e.NameScope.Find(XAxis_PART) as DateTimeAxis;

            Bind(MinimumVisibleTimeProperty, new Binding("VisibleDateRange.Minimum") { Mode = BindingMode.OneWay });
            Bind(MaximumVisibleTimeProperty, new Binding("VisibleDateRange.Maximum") { Mode = BindingMode.OneWay });

            Bind(ItemsProperty, new Binding("Items"));
            Bind(SelectedItemProperty, new Binding("SelectedItem"));

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

            //if (Plot != null)
            //{
            //    try
            //    {
            //        Plot.ApplyTemplate();
            //        //FixBugWithNotRegisteringMouseClickedOnPlotArea();
            //    }
            //    catch
            //    {
            //    }
            //}

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

            if (e.Property == ItemsProperty)
            {
                RefreshLines();
            }

            if (e.Property == SelectedItemProperty)
            {
                RefreshLines();
            }
        }

        protected virtual void RefreshLines()
        {
            var trendViewModels = Items;
            if (trendViewModels == null)
                return;

            ClearLines();

            var trendViewModelsInDisplayOrder = ItemsInDisplayOrder(trendViewModels);

            trendViewModelsInDisplayOrder
                .ToList()
                .ForEach(item => AddLine(item));
        }

        protected virtual IEnumerable<TrendViewModel> ItemsInDisplayOrder(IEnumerable<TrendViewModel> trendViewModels)
        {
            var trendViewModelsList = trendViewModels.ToList();
            IEnumerable<TrendViewModel> trendItemsInDisplayOrder;
            if (SelectedItem is not null)
                trendItemsInDisplayOrder = trendViewModelsList.Contains(SelectedItem)
                    ? trendViewModelsList.Except(new[] { SelectedItem }).Concat(new[] { SelectedItem })
                    : trendViewModelsList;
            else
                trendItemsInDisplayOrder = trendViewModelsList;
            return trendItemsInDisplayOrder;
        }

        protected virtual void ClearLines()
        {
            if (Plot != null)
            {
                Plot.Series.Clear();
            }
        }

        protected virtual LineSeries AddLine(TrendViewModel trendViewModel)
        {
            var lineSeries = new LineSeries
            {
                DataContext = trendViewModel,
                StrokeThickness = trendViewModel == SelectedItem ? 3 : 1,
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
                LineSeries.ItemsSourceProperty,
                new Binding("Points"));

            lineSeries.Bind(
                LineSeries.IsVisibleProperty,
                new Binding("IsDisplayedOnPlot"));

            lineSeries.Bind(
                LineSeries.ColorProperty,
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
    }
}
