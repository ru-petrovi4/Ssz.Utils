using Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews;

public class TrendGroupDsShapeView : DsShapeViewBase
{
    #region construction and destruction

    public TrendGroupDsShapeView(TrendGroupDsShape dsShape, ControlsPlay.Frame? frame)
        : base(dsShape, frame)
    {
        Control = new TrendGroupControl();
        Content = Control;
    }

    protected override void Dispose(bool disposing)
    {
        if (Disposed) return;

        if (disposing)
            Control.Dispose();

        base.Dispose(disposing);
    }

    #endregion

    #region protected functions

    protected TrendGroupControl Control { get; }

    protected override void OnDsShapeChanged(string? propertyName)
    {
        base.OnDsShapeChanged(propertyName);

        var dsShape = (TrendGroupDsShape) DsShapeViewModel.DsShape;

        if (propertyName is null || propertyName == nameof(dsShape.DsTrendItemsCollection))
        {
            Control.Jump(dsShape.DsTrendItemsCollection);
        }

        if (propertyName is null || propertyName == nameof(dsShape.Background))
        {
            Control.Background = dsShape.Background.GetBrush(dsShape.Container);
        }

        if (propertyName is null || propertyName == nameof(dsShape.ChartBackground))
        {
            if (Control.MainGenericTrendsPlotView.Plot is not null)
                Control.MainGenericTrendsPlotView.Plot.PlotAreaBackground =
                    dsShape.ChartBackground.GetBrush(dsShape.Container);
        }

        if (propertyName is null || propertyName == nameof(dsShape.TrendsInfoTableVisibility))
        {
            // Goes through the control so that the legend toggle button stays in sync.
            Control.IsTrendsInfoTableVisible = dsShape.TrendsInfoTableVisibility;
        }

        if (propertyName is null || propertyName == nameof(dsShape.TrendsAxisXVisibility))
        {
            if (Control.MainGenericTrendsPlotView.XAxis is not null)
                Control.MainGenericTrendsPlotView.XAxis.IsAxisVisible = dsShape.TrendsAxisXVisibility;
        }

        if (propertyName is null || propertyName == nameof(dsShape.TrendsAxisYVisibility))
        {
            if (Control.MainGenericTrendsPlotView.YAxis is not null)
                Control.MainGenericTrendsPlotView.YAxis.IsAxisVisible = dsShape.TrendsAxisYVisibility;
        }

        if (propertyName is null || propertyName == nameof(dsShape.TrendsScrollbarsVisibility))
        {
            if (Control.MainGenericTrendsPlotView.HorizontalScrollBar is not null)
                Control.MainGenericTrendsPlotView.HorizontalScrollBar.IsVisible =
                    dsShape.TrendsScrollbarsVisibility;
        }
    }

    #endregion
}
