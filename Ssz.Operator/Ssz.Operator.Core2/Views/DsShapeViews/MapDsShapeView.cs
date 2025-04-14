using System.Collections.Generic;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.ControlsPlay;
//using Ssz.Operator.Core.ControlsPlay.PanoramaPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Panorama;
namespace Ssz.Operator.Core.DsShapeViews
{
    //public class MapDsShapeView : DsShapeViewBase
    //{
    //    #region construction and destruction

    //    public MapDsShapeView(MapDsShape dsShape, Frame? frame)
    //        : base(dsShape, frame)
    //    {
    //        //SnapsToDevicePixels = false;

    //        _path = new Path();
    //        _path.Stretch = Stretch.None;
    //        Content = _path;

    //        _boundingRect = DsProject.Instance.GetAddon<PanoramaAddon>().PanoPointsCollection.GetBoundingRect();

    //        if (VisualDesignMode)
    //        {
    //            SizeChanged += (sender, args) => DrawMap();
    //        }
    //        else
    //        {
    //            IsHitTestVisible = false;

    //            DsProject.Instance.GetAddon<PanoramaAddon>().PanoPointsCollection.CurrentPathChanged +=
    //                PanoPointsCollectionOnCurrentPathChanged;

    //            Loaded += (sender, args) => PanoPointsCollectionOnCurrentPathChanged();
    //        }
    //    }

    //    #endregion

    //    #region protected functions

    //    protected override void OnDsShapeChanged(string? propertyName)
    //    {
    //        base.OnDsShapeChanged(propertyName);

    //        var dsShape = DsShapeViewModel.DsShape as MapDsShape;
    //        if (dsShape is null) return;

    //        if (propertyName is null || propertyName == nameof(dsShape.StrokeThickness))
    //            _path.SetConst(dsShape.Container, Shape.StrokeThicknessProperty, dsShape.StrokeThickness);
    //        if (propertyName is null || propertyName == nameof(dsShape.StrokeDashLength) ||
    //            propertyName == nameof(dsShape.StrokeGapLength))
    //        {
    //            if (dsShape.StrokeDashLength.HasValue && dsShape.StrokeGapLength.HasValue)
    //                _path.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
    //                    new DoubleCollection(new[]
    //                    {
    //                        dsShape.StrokeDashLength.Value,
    //                        dsShape.StrokeGapLength.Value
    //                    }));
    //            else
    //                _path.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
    //        }

    //        if (propertyName is null || propertyName == nameof(dsShape.StrokeInfo))
    //            _path.SetBindingOrConst(dsShape.Container, Shape.StrokeProperty, dsShape.StrokeInfo,
    //                BindingMode.OneWay,
    //                UpdateSourceTrigger.Default, VisualDesignMode);
    //    }

    //    #endregion

    //    #region private functions

    //    private void DrawMap()
    //    {
    //        if (double.IsNaN(Bounds.Width) || double.IsNaN(Bounds.Height)) return;

    //        PanoPointsCollection panoPointsCollection =
    //            DsProject.Instance.GetAddon<PanoramaAddon>().PanoPointsCollection;
    //        List<PanoPoint> panoPoints = panoPointsCollection.PanoPoints;
    //        if (panoPoints.Count == 0) return;

    //        var pathFigures = new List<PathFigure>();
    //        foreach (PanoPoint p in panoPoints)
    //        foreach (PanoPointRef r in p.PanoPointRefs)
    //        {
    //            var pathFigure = new PathFigure
    //            {
    //                StartPoint = GetPathPoint(p.X, p.Y),
    //                IsClosed = false,
    //                IsFilled = false
    //            };
    //            pathFigure.Segments.Add(new LineSegment
    //            {
    //                Point = GetPathPoint(r.ToPanoPoint.X,
    //                    r.ToPanoPoint.Y)
    //            });
    //            pathFigures.Add(pathFigure);
    //        }

    //        _path.SetConst(null, Path.DataProperty, new PathGeometry(pathFigures));
    //    }

    //    private void PanoPointsCollectionOnCurrentPathChanged()
    //    {
    //        PanoPointsCollection panoPointsCollection =
    //            DsProject.Instance.GetAddon<PanoramaAddon>().PanoPointsCollection;
    //        PanoPointRef[] currentPath = panoPointsCollection.CurrentPath;
    //        if (currentPath.Length == 0) return;

    //        var pathFigure = new PathFigure
    //        {
    //            StartPoint = GetPathPoint(currentPath[0].ParentPanoPoint.X,
    //                currentPath[0].ParentPanoPoint.Y),
    //            IsClosed = false,
    //            IsFilled = false
    //        };
    //        foreach (PanoPointRef r in currentPath)
    //            pathFigure.Segments.Add(new LineSegment
    //            {
    //                Point = GetPathPoint(r.ToPanoPoint.X,
    //                    r.ToPanoPoint.Y)
    //            });
    //        _path.SetConst(null, Path.DataProperty, new PathGeometry(new[] {pathFigure}));
    //    }

    //    private Point GetPathPoint(double x, double y)
    //    {
    //        var kX = Bounds.Width / _boundingRect.Width;
    //        var kY = Bounds.Height / _boundingRect.Height;
    //        return new Point(kX * (x - _boundingRect.X), Bounds.Height - kY * (y - _boundingRect.Y));
    //    }

    //    #endregion

    //    #region private fields

    //    private readonly Path _path;
    //    private readonly Rect _boundingRect;

    //    #endregion
    //}
}