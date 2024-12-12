using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsDesign
{
    public partial class DesignDrawingCanvas : Canvas
    {
        #region private fields

        private readonly CaseInsensitiveDictionary<ConnectionPointInfo> _connectionPoints = new();

        #endregion

        #region public functions

        public ConnectionPointDsShapeView? GetConnectionPointDsShapeViewAt(Point point)
        {
            foreach (var connectionPointInfo in _connectionPoints.Values)
            {
                var connectionPointDsShapeView = connectionPointInfo.ConnectionPointDsShapeView;
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
                foreach (var cpsv in complexDsShapeView.ConnectionPointDsShapeViews)
                {
                    var connectionPointPath = cpsv.DsShapeViewModel.DsShape.GetDsShapePath();
                    if (!string.IsNullOrEmpty(connectionPointPath))
                    {
                        ConnectionPointInfo connectionPointInfo = GetConnectionPointInfo(connectionPointPath);
                        connectionPointInfo.ConnectionPointDsShapeView =
                            cpsv;
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
                foreach (var cpsv in complexDsShapeView.ConnectionPointDsShapeViews)
                {
                    var connectionPointPath = cpsv.DsShapeViewModel.DsShape.GetDsShapePath();
                    if (!string.IsNullOrEmpty(connectionPointPath))
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
            // TODO
            //foreach (var cpsv in ((ComplexDsShapeView) ((DesignDsShapeView) (complexDsShape.TagObject ??
            //                         throw new InvalidOperationException())).DsShapeView)
            //                     .ConnectionPointDsShapeViews ??
            //                     throw new InvalidOperationException())
            //{
            //    var connectionPointInfo = cpsv.ConnectionPointInfo;
            //    if (connectionPointInfo is null) continue;

            //    var pointOnDrawing = cpsv.DsShapeViewModel.DsShape.GetCenterInitialPositionOnDrawing();

            //    foreach (DesignConnectorDsShapeView dcsv in connectionPointInfo.BeginDesignConnectorDsShapeViews)
            //        dcsv.OnBeginChanged(pointOnDrawing);

            //    foreach (DesignConnectorDsShapeView dcsv in connectionPointInfo.EndDesignConnectorDsShapeViews)
            //        dcsv.OnEndChanged(pointOnDrawing);
            //}
        }

        #endregion
    }

    public class ConnectionPointInfo
    {
        public readonly List<DesignConnectorDsShapeView> BeginDesignConnectorDsShapeViews = new();


        public readonly List<DesignConnectorDsShapeView> EndDesignConnectorDsShapeViews = new();
        public ConnectionPointDsShapeView? ConnectionPointDsShapeView;
    }
}