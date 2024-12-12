using System;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    public static class DesignDsShapeViewFactory
    {
        #region public functions

        public static DesignDsShapeView New(DsShapeViewBase dsShapeView,
            DesignDrawingCanvas designerDrawingCanvas)
        {
            var connectorDsShapeView = dsShapeView as ConnectorDsShapeView;
            if (connectorDsShapeView is not null)
                return new DesignConnectorDsShapeView(connectorDsShapeView, designerDrawingCanvas);

            var geometryDsShapeView = dsShapeView as IGeometryDsShapeView;
            if (geometryDsShapeView is not null)
                return new DesignGeometryDsShapeView(geometryDsShapeView, designerDrawingCanvas);

            return new DesignDsShapeView(dsShapeView, designerDrawingCanvas);
        }

        #endregion
    }
}