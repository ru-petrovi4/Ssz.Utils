using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign.GeometryEditing
{
    /// <summary>
    ///     The handle at one end of a connector. Dropping it on a connection point of a shape connects
    ///     the two; dropping it anywhere else disconnects that end.
    /// </summary>
    public class ConnectorTerminalControlPoint : ControlPoint
    {
        public enum Type
        {
            Begin,
            End
        }

        #region construction and destruction

        public ConnectorTerminalControlPoint(DesignConnectorDsShapeView designerConnectorDsShapeView, Type type,
            PathControlPoint cp0, PathControlPoint cp1) :
            base(designerConnectorDsShapeView)
        {
            _type = type;
            _cp0 = cp0;
            _cp1 = cp1;

            _cp0.CenterChanged += () => { Center = _cp0.Center; };

            Center = _cp0.Center;

            ZIndex = 1;
        }

        #endregion

        #region public functions

        public override double Radius => 4.0;

        public override void DragObject(Point point)
        {
            var connectorDsShape = DesignGeometryDsShapeView.DsShapeViewModel.DsShape;
            Point drawingPoint = connectorDsShape.GetDrawingPoint(point);
            ConnectionPointDsShapeView? connectionPointDsShapeView =
                DesignGeometryDsShapeView.DesignDrawingCanvas.GetConnectionPointDsShapeViewAt(drawingPoint);
            var connected = SetConnectorDsShapeBeginEnd(connectionPointDsShapeView);

            if (connectionPointDsShapeView is not null)
            {
                if (connected)
                {
                    var connectionPointDsShape =
                        (ConnectionPointDsShape) connectionPointDsShapeView.DsShapeViewModel.DsShape;
                    point = connectorDsShape.GetDsShapePoint(connectionPointDsShape
                        .GetCenterInitialPositionOnDrawing());
                }
                else
                {
                    return;
                }
            }

            SetUnderlyingCenter(point);
        }

        public override ContextMenu? GetContextMenu()
        {
            return null;
        }

        public override void SetUnderlyingCenter(Point point)
        {
            Point p0 = _cp0.Center;
            Point p1 = _cp1.Center;
            var dpX = p1.X - p0.X;
            var dpY = p1.Y - p0.Y;
            var deltaX = 0.0;
            var deltaY = 0.0;
            if (dpY > -0.001 && dpY < 0.001)
                deltaY = point.Y - p0.Y;
            else if (dpX > -0.001 && dpX < 0.001) deltaX = point.X - p0.X;
            _cp0.SetUnderlyingCenter(point);
            _cp1.SetUnderlyingCenter(new Point(p1.X + deltaX, p1.Y + deltaY));
        }

        #endregion

        #region private functions

        private bool SetConnectorDsShapeBeginEnd(ConnectionPointDsShapeView? connectionPointDsShapeView)
        {
            var connected = false;

            var connectorDsShape =
                (ConnectorDsShape) DesignGeometryDsShapeView.GeometryDsShapeView.DsShapeViewModel.DsShape;

            if (connectionPointDsShapeView is not null)
            {
                var connectionPointDsShape =
                    (ConnectionPointDsShape) connectionPointDsShapeView.DsShapeViewModel.DsShape;

                switch (connectionPointDsShape.Type)
                {
                    case ConnectionPointDsShapeType.In:
                        connected = _type == Type.End;
                        break;
                    case ConnectionPointDsShapeType.Out:
                        connected = _type == Type.Begin;
                        break;
                    default:
                        connected = true;
                        break;
                }

                if (connected)
                    switch (_type)
                    {
                        case Type.Begin:
                            connectorDsShape.Begin = connectionPointDsShape.GetDsShapePath();
                            break;
                        case Type.End:
                            connectorDsShape.End = connectionPointDsShape.GetDsShapePath();
                            break;
                    }
            }

            if (!connected)
                switch (_type)
                {
                    case Type.Begin:
                        connectorDsShape.Begin = @"";
                        break;
                    case Type.End:
                        connectorDsShape.End = @"";
                        break;
                }

            return connected;
        }

        #endregion

        #region private fields

        private readonly Type _type;
        private readonly PathControlPoint _cp0;
        private readonly PathControlPoint _cp1;

        #endregion
    }
}
