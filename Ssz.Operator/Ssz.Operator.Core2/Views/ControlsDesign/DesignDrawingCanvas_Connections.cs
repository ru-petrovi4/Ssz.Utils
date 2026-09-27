using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     What the canvas knows about connection points: which shape owns each one, and which
    ///     connectors start or end at it.
    /// </summary>
    public partial class DesignDrawingCanvas : Canvas
    {
        #region public functions

        public ConnectionPointDsShapeView? GetConnectionPointDsShapeViewAt(Point point)
        {
            foreach (ConnectionPointInfo connectionPointInfo in _connectionPoints.Values)
            {
                ConnectionPointDsShapeView? connectionPointDsShapeView =
                    connectionPointInfo.ConnectionPointDsShapeView;
                if (connectionPointDsShapeView is not null &&
                    connectionPointDsShapeView.DsShapeViewModel.DsShape.Contains(point, false))
                    return connectionPointDsShapeView;
            }

            return null;
        }

        public ConnectionPointInfo GetConnectionPointInfo(string connectionPointPath)
        {
            ConnectionPointInfo? result;
            if (!_connectionPoints.TryGetValue(connectionPointPath, out result))
            {
                result = new ConnectionPointInfo();
                _connectionPoints.Add(connectionPointPath, result);
            }

            return result;
        }

        #endregion

        #region private functions

        private void DrawingDesignDsShapeViewAdded(DesignDsShapeView designerDsShapeView)
        {
            var complexDsShapeView = designerDsShapeView.DsShapeView as ComplexDsShapeView;
            if (complexDsShapeView is not null)
            {
                complexDsShapeView.DsShapeViewModel.DsShape.PropertyChanged +=
                    ComplexDsShapeOnPropertyChanged;

                if (complexDsShapeView.ConnectionPointDsShapeViews is null) throw new InvalidOperationException();
                foreach (ConnectionPointDsShapeView cpsv in complexDsShapeView.ConnectionPointDsShapeViews)
                {
                    var connectionPointPath = cpsv.DsShapeViewModel.DsShape.GetDsShapePath();
                    if (!String.IsNullOrEmpty(connectionPointPath))
                    {
                        ConnectionPointInfo connectionPointInfo = GetConnectionPointInfo(connectionPointPath);
                        connectionPointInfo.ConnectionPointDsShapeView = cpsv;
                        cpsv.ConnectionPointInfo = connectionPointInfo;
                    }
                }
            }
        }

        private void DrawingDesignDsShapeViewRemoved(DesignDsShapeView designerDsShapeView)
        {
            var complexDsShapeView = designerDsShapeView.DsShapeView as ComplexDsShapeView;
            if (complexDsShapeView is not null)
            {
                complexDsShapeView.DsShapeViewModel.DsShape.PropertyChanged -=
                    ComplexDsShapeOnPropertyChanged;
                if (complexDsShapeView.ConnectionPointDsShapeViews is null) throw new InvalidOperationException();
                foreach (ConnectionPointDsShapeView cpsv in complexDsShapeView.ConnectionPointDsShapeViews)
                {
                    var connectionPointPath = cpsv.DsShapeViewModel.DsShape.GetDsShapePath();
                    if (!String.IsNullOrEmpty(connectionPointPath))
                    {
                        ConnectionPointInfo connectionPointInfo = GetConnectionPointInfo(connectionPointPath);
                        connectionPointInfo.ConnectionPointDsShapeView = null;
                        cpsv.ConnectionPointInfo = null;
                    }
                }
            }
        }

        private void ComplexDsShapeOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
        {
            if (sender is null) return;
            var complexDsShape = (ComplexDsShape) sender;
            if (complexDsShape.Disposed) return;
            if (args.PropertyName == @"CenterInitialPosition")
                ComplexDsShapeOnCenterInitialPositionChanged(complexDsShape);
        }

        private void ComplexDsShapeOnCenterInitialPositionChanged(ComplexDsShape complexDsShape)
        {
            // TODO: the WPF version dragged the connectors along with the shape they are attached to.
            // It was already switched off there and is left off here for the same reason.
        }

        #endregion

        #region private fields

        private readonly CaseInsensitiveOrderedDictionary<ConnectionPointInfo> _connectionPoints = new();

        #endregion
    }

    public class ConnectionPointInfo
    {
        public readonly List<DesignConnectorDsShapeView> BeginDesignConnectorDsShapeViews = new();

        public readonly List<DesignConnectorDsShapeView> EndDesignConnectorDsShapeViews = new();

        public ConnectionPointDsShapeView? ConnectionPointDsShapeView;
    }
}
