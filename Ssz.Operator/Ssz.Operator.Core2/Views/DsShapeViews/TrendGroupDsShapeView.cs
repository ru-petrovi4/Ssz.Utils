using System.Collections.ObjectModel;
using Avalonia;
using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends;


//using Ssz.Operator.Core.ControlsCommon.Trends;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapes.Trends;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class TrendGroupDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public TrendGroupDsShapeView(TrendGroupDsShape dsShape, Frame? frame)
            : base(dsShape, frame)
        {
            Control = new TrendGroupControl();
            Content = Control;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            //if (disposing)
            //    if (Control.TrendItemViewsCollection is not null)
            //        foreach (Trend trendItemView in Control.TrendItemViewsCollection)
            //        {
            //            trendItemView.Dispose();
            //        }

            base.Dispose(disposing);
        }

        #endregion

        #region protected functions

        protected TrendGroupControl Control { get; }

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (TrendGroupDsShape)DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.DsTrendItemsCollection))
            {
                Control.Jump(dsShape.DsTrendItemsCollection);
            }

            if (propertyName is null || propertyName == nameof(dsShape.ChartBackground))
            {
                if (Control.MainGenericTrendsPlotView.Plot is not null)
                    Control.MainGenericTrendsPlotView.Plot.PlotAreaBackground =
                        dsShape.ChartBackground.GetBrush(dsShape.Container);
            }
            //if (propertyName is null || propertyName == nameof(dsShape.ChartGridBrush))
            //    Control.MainGenericTrendsPlotView.Plot.PlotGridBrush =
            //        dsShape.ChartGridBrush.GetBrush(dsShape.Container);
            //if (propertyName is null || propertyName == nameof(dsShape.ChartAxisBrush))
            //    Control.MainGenericTrendsPlotView.AxisBrush = dsShape.ChartAxisBrush.GetBrush(dsShape.Container);
            //if (propertyName is null || propertyName == nameof(dsShape.Background))
            //    Control.TrendsInfoTableControl.Background =
            //        Control.Background = dsShape.Background.GetBrush(dsShape.Container);
            //if (propertyName is null || propertyName == nameof(dsShape.TrendsInfoTableVisibility))
            //{
            //    if (dsShape.TrendsInfoTableVisibility)
            //        Control.TrendsInfoTableControl.Visibility = true;
            //    else
            //        Control.TrendsInfoTableControl.Visibility = false;
            //}
            //if (propertyName is null || propertyName == nameof(dsShape.TrendsTuningVisibility))
            //{
            //    if (dsShape.TrendsTuningVisibility)
            //        Control.TrendsTuningControl.Visibility = true;
            //    else
            //        Control.TrendsTuningControl.Visibility = false;
            //}
            //if (propertyName is null || propertyName == nameof(dsShape.TrendsAxisXVisibility))
            //{
            //    if (dsShape.TrendsAxisXVisibility)
            //        Control.MainGenericTrendsPlotView.XAxis.IsVisible = true;
            //    else
            //        Control.MainGenericTrendsPlotView.Plot.HorizontalAxis.Visibility = false;
            //}
            //if (propertyName is null || propertyName == nameof(dsShape.TrendsAxisYVisibility))
            //{
            //    if (dsShape.TrendsAxisYVisibility)
            //        Control.MainGenericTrendsPlotView.Plot.VerticalAxis.Visibility = true;
            //    else
            //        Control.MainGenericTrendsPlotView.Plot.VerticalAxis.Visibility = false;
            //}
            //if (propertyName is null || propertyName == nameof(dsShape.TrendsScrollbarsVisibility))
            //{
            //    if (dsShape.TrendsScrollbarsVisibility)
            //    {
            //        Control.MainGenericTrendsPlotView.HorizontalScrollBar.Visibility = true;
            //        Control.MainGenericTrendsPlotView.VerticalScrollBar.Visibility = true;
            //    }
            //    else
            //    {
            //        Control.MainGenericTrendsPlotView.HorizontalScrollBar.Visibility = false;
            //        Control.MainGenericTrendsPlotView.VerticalScrollBar.Visibility = false;
            //    }
            //}
        }

        #endregion
    }
}