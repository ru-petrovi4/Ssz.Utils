using System.Collections.ObjectModel;
using Avalonia;
//using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapes.Trends;

namespace Ssz.Operator.Core.DsShapeViews
{
    //public class TrendGroupDsShapeView : DsShapeViewBase
    //{
    //    #region construction and destruction

    //    public TrendGroupDsShapeView(TrendGroupDsShape dsShape, Frame? frame)
    //        : base(dsShape, frame)
    //    {
    //        Control = new TrendGroupControl();
    //        Content = Control;
    //    }

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (Disposed) return;

    //        if (disposing)
    //            if (Control.TrendItemViewsCollection is not null)
    //                foreach (Trend trendItemView in Control.TrendItemViewsCollection)
    //                {
    //                    trendItemView.Dispose();
    //                }

    //        base.Dispose(disposing);
    //    }

    //    #endregion

    //    #region protected functions

    //    protected TrendGroupControl Control { get; }

    //    protected override void OnDsShapeChanged(string? propertyName)
    //    {
    //        base.OnDsShapeChanged(propertyName);

    //        var dsShape = (TrendGroupDsShape) DsShapeViewModel.DsShape;
    //        if (propertyName is null || propertyName == nameof(dsShape.DsTrendItemsCollection))
    //        {
    //            if (Control.TrendItemViewsCollection is not null)
    //                foreach (Trend trendItemView in Control.TrendItemViewsCollection)
    //                {
    //                    trendItemView.Dispose();
    //                }

    //            var trendItemViews = new ObservableCollection<Trend>();
    //            foreach (DsTrendItem dsTrendItem in dsShape.DsTrendItemsCollection)
    //            {
    //                dsTrendItem.ParentItem = dsShape;
    //                var trendItemView = new Trend(dsTrendItem, true, true,
    //                    Frame?.PlayWindow);                    
    //                trendItemViews.Add(trendItemView);
    //            }

    //            Control.TrendItemViewsCollection = trendItemViews;
    //        }

    //        if (propertyName is null || propertyName == nameof(dsShape.ChartBackground))
    //            Control.TrendChartPlotter.ChartPlotterBackground =
    //                dsShape.ChartBackground.GetBrush(dsShape.Container);
    //        if (propertyName is null || propertyName == nameof(dsShape.ChartGridBrush))
    //            Control.TrendChartPlotter.ChartPlotterGridBrush =
    //                dsShape.ChartGridBrush.GetBrush(dsShape.Container);
    //        if (propertyName is null || propertyName == nameof(dsShape.ChartAxisBrush))
    //            Control.TrendChartPlotter.AxisBrush = dsShape.ChartAxisBrush.GetBrush(dsShape.Container);
    //        if (propertyName is null || propertyName == nameof(dsShape.Background))
    //            Control.TrendsInfoTableControl.Background =
    //                Control.Background = dsShape.Background.GetBrush(dsShape.Container);
    //        if (propertyName is null || propertyName == nameof(dsShape.TrendsInfoTableVisibility))
    //        {
    //            if (dsShape.TrendsInfoTableVisibility)
    //                Control.TrendsInfoTableControl.Visibility = true;
    //            else
    //                Control.TrendsInfoTableControl.Visibility = false;
    //        }
    //        if (propertyName is null || propertyName == nameof(dsShape.TrendsTuningVisibility))
    //        {
    //            if (dsShape.TrendsTuningVisibility)
    //                Control.TrendsTuningControl.Visibility = true;
    //            else
    //                Control.TrendsTuningControl.Visibility = false;
    //        }
    //        if (propertyName is null || propertyName == nameof(dsShape.TrendsAxisXVisibility))
    //        {
    //            if (dsShape.TrendsAxisXVisibility)
    //                Control.TrendChartPlotter.ChartPlotter.HorizontalAxis.Visibility = true;
    //            else
    //                Control.TrendChartPlotter.ChartPlotter.HorizontalAxis.Visibility = false;
    //        }
    //        if (propertyName is null || propertyName == nameof(dsShape.TrendsAxisYVisibility))
    //        {
    //            if (dsShape.TrendsAxisYVisibility)
    //                Control.TrendChartPlotter.ChartPlotter.VerticalAxis.Visibility = true;
    //            else
    //                Control.TrendChartPlotter.ChartPlotter.VerticalAxis.Visibility = false;
    //        }
    //        if (propertyName is null || propertyName == nameof(dsShape.TrendsScrollbarsVisibility))
    //        {
    //            if (dsShape.TrendsScrollbarsVisibility)
    //            {
    //                Control.TrendChartPlotter.HorizontalScrollBar.Visibility = true;
    //                Control.TrendChartPlotter.VerticalScrollBar.Visibility = true;
    //            }
    //            else
    //            {
    //                Control.TrendChartPlotter.HorizontalScrollBar.Visibility = false;
    //                Control.TrendChartPlotter.VerticalScrollBar.Visibility = false;
    //            }
    //        }
    //    }

    //    #endregion
    //}
}