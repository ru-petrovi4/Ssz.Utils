using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ConnectorDsShapeView : GeometryDsShapeView
    {
        #region construction and destruction

        public ConnectorDsShapeView(ConnectorDsShape dsShape, Frame? frame)
            : base(dsShape, frame)
        {
        }

        #endregion
    }
}