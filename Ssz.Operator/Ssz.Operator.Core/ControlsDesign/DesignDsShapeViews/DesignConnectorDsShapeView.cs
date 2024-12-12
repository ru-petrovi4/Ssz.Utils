using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignConnectorDsShapeView : DesignGeometryDsShapeView
    {
        #region protected functions

        protected override void CreateControlPoints()
        {
            var pathGeometry = GeometryDsShapeView.Geometry as PathGeometry;
            if (pathGeometry is null) return;

            PathControlPoint? lastPathControlPoint = null;

            pathGeometry.DoForAllPointDependencyProperties((obj, dp) =>
            {
                if (dp.PropertyType == typeof(Point))
                {
                    var pathControlPoint = new PathControlPoint(this, obj, dp, 0);
                    ControlPointsAdd(pathControlPoint);

                    lastPathControlPoint = pathControlPoint;
                }
                else if (dp.PropertyType == typeof(PointCollection))
                {
                    var pointCollection = (PointCollection) obj.GetValue(dp);
                    for (var index = 0; index < pointCollection.Count; index += 1)
                    {
                        var pt = pointCollection[index];
                        var pathControlPoint = new PathControlPoint(this, obj, dp, index);
                        ControlPointsAdd(pathControlPoint);

                        if (lastPathControlPoint is not null)
                        {
                            if (index == 0)
                            {
                                _beginConnectorTerminalControlPoint = new ConnectorTerminalControlPoint(this,
                                    ConnectorTerminalControlPoint.Type.Begin, lastPathControlPoint, pathControlPoint);
                                ControlPointsAdd(_beginConnectorTerminalControlPoint);
                            }

                            if (index == pointCollection.Count - 1)
                            {
                                _endConnectorTerminalControlPoint = new ConnectorTerminalControlPoint(this,
                                    ConnectorTerminalControlPoint.Type.End, pathControlPoint, lastPathControlPoint);
                                ControlPointsAdd(_endConnectorTerminalControlPoint);
                            }

                            if (index > 0 && index < pointCollection.Count - 1)
                                ControlPointsAdd(new MiddleControlPoint(this, lastPathControlPoint, pathControlPoint));
                        }

                        lastPathControlPoint = pathControlPoint;
                    }
                }
            });
        }

        #endregion

        #region construction and destruction

        public DesignConnectorDsShapeView(ConnectorDsShapeView connectorDsShapeView,
            DesignDrawingCanvas designerDrawingCanvas) :
            base(connectorDsShapeView, designerDrawingCanvas)
        {
            DsShapeViewModel.ResizeDecoratorIsVisible = false;

            _connectorDsShape = (ConnectorDsShape) connectorDsShapeView.DsShapeViewModel.DsShape;

            _connectorDsShape.PropertyChanged += ConnectorDsShapeOnPropertyChanged;
            OnBeginChanged(_connectorDsShape.Begin);
            OnEndChanged(_connectorDsShape.End);
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _connectorDsShape.PropertyChanged -= ConnectorDsShapeOnPropertyChanged;
                OnBeginChanged(null);
                OnEndChanged(null);
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public void OnBeginChanged(Point point)
        {
            if (DsShapeViewModel.IsSelected || _beginConnectorTerminalControlPoint is null) return;

            point = DsShapeViewModel.DsShape.GetDsShapePoint(point);

            _beginConnectorTerminalControlPoint.SetUnderlyingCenter(point);
        }


        public void OnEndChanged(Point point)
        {
            if (DsShapeViewModel.IsSelected || _endConnectorTerminalControlPoint is null) return;

            point = DsShapeViewModel.DsShape.GetDsShapePoint(point);

            _endConnectorTerminalControlPoint.SetUnderlyingCenter(point);
        }

        #endregion

        #region private functions

        private void ConnectorDsShapeOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == @"Begin")
                OnBeginChanged(_connectorDsShape.Begin);
            else if (args.PropertyName == @"End") OnEndChanged(_connectorDsShape.End);
        }

        private void OnBeginChanged(string? path)
        {
            if (_beginConnectionPointInfo is not null)
                _beginConnectionPointInfo.BeginDesignConnectorDsShapeViews.Remove(this);
            if (!string.IsNullOrEmpty(path))
            {
                _beginConnectionPointInfo = DesignDrawingCanvas.GetConnectionPointInfo(path!);
                _beginConnectionPointInfo.BeginDesignConnectorDsShapeViews.Add(this);
            }
            else
            {
                _beginConnectionPointInfo = null;
            }
        }

        private void OnEndChanged(string? path)
        {
            if (_endConnectionPointInfo is not null) _endConnectionPointInfo.EndDesignConnectorDsShapeViews.Remove(this);
            if (!string.IsNullOrEmpty(path))
            {
                _endConnectionPointInfo = DesignDrawingCanvas.GetConnectionPointInfo(path!);
                _endConnectionPointInfo.EndDesignConnectorDsShapeViews.Add(this);
            }
            else
            {
                _endConnectionPointInfo = null;
            }
        }

        #endregion

        #region private fields

        private readonly ConnectorDsShape _connectorDsShape;
        private ConnectionPointInfo? _beginConnectionPointInfo;
        private ConnectionPointInfo? _endConnectionPointInfo;
        private ConnectorTerminalControlPoint? _beginConnectorTerminalControlPoint;
        private ConnectorTerminalControlPoint? _endConnectorTerminalControlPoint;

        #endregion
    }
}