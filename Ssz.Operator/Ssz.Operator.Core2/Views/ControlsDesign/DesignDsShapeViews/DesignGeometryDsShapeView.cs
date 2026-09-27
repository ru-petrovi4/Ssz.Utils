using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     A shape whose outline the author can edit point by point. On top of the shape it draws the
    ///     handles: one per point of the path, plus one between each pair for moving a whole segment.
    /// </summary>
    public class DesignGeometryDsShapeView : DesignDsShapeView
    {
        #region construction and destruction

        public DesignGeometryDsShapeView(IGeometryDsShapeView geometryDsShapeView,
            DesignDrawingCanvas designerDrawingCanvas) :
            base((DsShapeViewBase) geometryDsShapeView, designerDrawingCanvas)
        {
            ControlPointsGeometryGroup = new GeometryGroup();
            ControlPointsGeometryGroup.FillRule = FillRule.EvenOdd;
            _controlPointsPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = 1.0,
                Fill = new SolidColorBrush(Colors.White),
                Opacity = 0.5,
                Data = ControlPointsGeometryGroup,
                ZIndex = 1
            };

            SelectedControlPointGeometryGroup = new GeometryGroup();
            _selectedControlPointPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = 1.0,
                Fill = new SolidColorBrush(Colors.Blue),
                Opacity = 0.5,
                Data = SelectedControlPointGeometryGroup,
                ZIndex = 2
            };

            var canvas = new Canvas();
            canvas.Children.Add(_controlPointsPath);
            canvas.Children.Add(_selectedControlPointPath);
            Content = canvas;

            GeometryDsShapeView.GeometryChanged += OnGeometryChanged;
            OnGeometryChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing) GeometryDsShapeView.GeometryChanged -= OnGeometryChanged;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public IGeometryDsShapeView GeometryDsShapeView => (IGeometryDsShapeView) DsShapeView;

        public static readonly StyledProperty<ControlPoint?> SelectedControlPointProperty =
            AvaloniaProperty.Register<DesignGeometryDsShapeView, ControlPoint?>(nameof(SelectedControlPoint));

        public ControlPoint? SelectedControlPoint
        {
            get => GetValue(SelectedControlPointProperty);
            set
            {
                ControlPoint? selectedControlPoint = GetValue(SelectedControlPointProperty);
                if (selectedControlPoint != value)
                {
                    if (selectedControlPoint is not null) selectedControlPoint.IsSelected = false;
                    if (value is not null)
                    {
                        value.IsSelected = true;
                        _selectedControlPointPath.ContextMenu = value.GetContextMenu();
                    }
                    else
                    {
                        _selectedControlPointPath.ContextMenu = null;
                    }

                    SetValue(SelectedControlPointProperty, value);
                }
            }
        }

        public void ClearControlPoints()
        {
            ControlPointsGeometryGroup.Children.Clear();
            SelectedControlPointGeometryGroup.Children.Clear();

            SelectedControlPoint = null;
            for (var i = _controlPoints.Count - 1; i >= 0; i--) _controlPoints[i].Dispose();
            _controlPoints.Clear();
        }

        public void ControlPointsAdd(ControlPoint controlPoint)
        {
            controlPoint.Num = _controlPoints.Count;
            _controlPoints.Add(controlPoint);
        }

        public IEnumerable<ControlPoint> ControlPointsOrdered =>
            _controlPoints.OrderByDescending(cp => cp.ZIndex).ThenByDescending(cp => cp.Num);

        public GeometryGroup ControlPointsGeometryGroup { get; }

        public GeometryGroup SelectedControlPointGeometryGroup { get; }

        /// <summary>
        ///     Whether the point is on the drawn outline rather than on the empty space around it.
        /// </summary>
        /// <summary>
        ///     Whether the outline of the shape is under this point, which is what decides whether a
        ///     click inside the box of an L or a circle counts as a click on the shape.
        ///     <para>
        ///         The WPF editor asked the visual tree with VisualTreeHelper.HitTest, which answers
        ///         for a shape view that takes no input. Avalonia only hit tests what can be pointed
        ///         at, and every shape view of a drawing being edited is out of the pointer's reach,
        ///         so the question is put to the geometry itself. The path stretches its geometry over
        ///         the whole shape, so the point travels the other way first.
        ///     </para>
        /// </summary>
        public bool HitTestPath(Point dsShapePoint)
        {
            Geometry? geometry = GeometryDsShapeView.Geometry;
            if (geometry is null) return false;

            DsShapeBase dsShape = DsShapeViewModel.DsShape;
            var width = dsShape.WidthInitialNotRounded;
            var height = dsShape.HeightInitialNotRounded;
            if (width <= 0.0 || height <= 0.0) return false;

            Rect bounds = geometry.Bounds;
            if (bounds.Width <= 0.0 || bounds.Height <= 0.0) return false;

            var scaleX = bounds.Width / width;
            var scaleY = bounds.Height / height;
            var geometryPoint = new Point(bounds.X + dsShapePoint.X * scaleX,
                bounds.Y + dsShapePoint.Y * scaleY);

            if (geometry.FillContains(geometryPoint)) return true;

            // An outline is given a band wide enough to be pointed at, as the WPF editor did.
            var thickness = GrabBandThickness * (scaleX + scaleY) / 2.0;
            return geometry.StrokeContains(new Pen(Brushes.Black, thickness), geometryPoint);
        }

        private const double GrabBandThickness = 5.0;

        public void AddPoint(PathControlPoint pathControlPoint)
        {
            var pathGeometry = GeometryDsShapeView.Geometry as PathGeometry;
            if (pathGeometry is null) return;

            switch (pathControlPoint.Type)
            {
                case PathControlPointType.FigureStartPoint:
                {
                    PathSegment newPathSegment =
                        GetNewPathSegment((Point) pathControlPoint.Obj.GetValue(pathControlPoint.AvaloniaProperty)!);

                    var pathFigure = (PathFigure) pathControlPoint.Obj;
                    if (pathFigure.Segments is null) return;

                    ClearControlPoints();
                    pathFigure.Segments.Insert(0, newPathSegment);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentInPointsCollection:
                {
                    var points =
                        pathControlPoint.Obj.GetValue(pathControlPoint.AvaloniaProperty) as IList<Point>;
                    if (points is null || pathControlPoint.Index >= points.Count) return;
                    Point point = points[pathControlPoint.Index];

                    ClearControlPoints();
                    points.Insert(pathControlPoint.Index, point);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentOtherPoint:
                {
                    var pathSegment = (PathSegment) pathControlPoint.Obj;

                    foreach (PathFigure f in pathGeometry.Figures ?? Enumerable.Empty<PathFigure>())
                    {
                        if (f.Segments is null) continue;
                        var i = f.Segments.IndexOf(pathSegment);
                        if (i > -1)
                        {
                            PathSegment newPathSegment = GetNewPathSegment(pathSegment.GetSegmentEndPoint());

                            ClearControlPoints();
                            f.Segments.Insert(i + 1, newPathSegment);
                            GeometryDsShapeView.UpdateModelLayer();

                            break;
                        }
                    }
                }
                    break;
            }
        }

        public void DeletePoint(PathControlPoint pathControlPoint)
        {
            var pathGeometry = GeometryDsShapeView.Geometry as PathGeometry;
            if (pathGeometry is null) return;

            switch (pathControlPoint.Type)
            {
                case PathControlPointType.FigureStartPoint:
                {
                    var pathFigure = (PathFigure) pathControlPoint.Obj;

                    ClearControlPoints();
                    pathGeometry.Figures?.Remove(pathFigure);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentInPointsCollection:
                {
                    var points =
                        pathControlPoint.Obj.GetValue(pathControlPoint.AvaloniaProperty) as IList<Point>;
                    if (points is null || pathControlPoint.Index >= points.Count) return;

                    ClearControlPoints();
                    points.RemoveAt(pathControlPoint.Index);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentOtherPoint:
                {
                    var pathSegment = (PathSegment) pathControlPoint.Obj;
                    PathFigure? pathFigure = null;
                    foreach (PathFigure f in pathGeometry.Figures ?? Enumerable.Empty<PathFigure>())
                        if (f.Segments is not null && f.Segments.Contains(pathSegment))
                        {
                            pathFigure = f;
                            break;
                        }

                    if (pathFigure?.Segments is not null)
                    {
                        ClearControlPoints();
                        pathFigure.Segments.Remove(pathSegment);
                        GeometryDsShapeView.UpdateModelLayer();
                    }
                }
                    break;
            }
        }

        #endregion

        #region protected functions

        protected virtual void CreateControlPoints()
        {
            var pathGeometry = GeometryDsShapeView.Geometry as PathGeometry;
            if (pathGeometry is null) return;

            PathControlPoint? lastPathControlPoint = null;

            pathGeometry.DoForAllPointProperties((obj, avaloniaProperty) =>
            {
                if (avaloniaProperty.PropertyType == typeof(Point))
                {
                    var pathControlPoint = new PathControlPoint(this, obj, avaloniaProperty, 0);
                    if (pathControlPoint.Type == PathControlPointType.FigureStartPoint && _controlPoints.Count > 0)
                        ControlPointsFinalizeFigure();
                    ControlPointsAdd(pathControlPoint);

                    if (lastPathControlPoint is not null)
                        ControlPointsAdd(new MiddleControlPoint(this, lastPathControlPoint, pathControlPoint));

                    lastPathControlPoint = pathControlPoint;
                }
                else
                {
                    var points = obj.GetValue(avaloniaProperty) as IList<Point>;
                    if (points is null) return;
                    for (var index = 0; index < points.Count; index += 1)
                    {
                        var pathControlPoint = new PathControlPoint(this, obj, avaloniaProperty, index);
                        ControlPointsAdd(pathControlPoint);

                        if (lastPathControlPoint is not null)
                            ControlPointsAdd(new MiddleControlPoint(this, lastPathControlPoint, pathControlPoint));

                        lastPathControlPoint = pathControlPoint;
                    }
                }
            });
            ControlPointsFinalizeFigure();
        }

        #endregion

        #region private functions

        private PathSegment GetNewPathSegment(Point point)
        {
            return new LineSegment { Point = point };
        }

        private void OnGeometryChanged()
        {
            ClearControlPoints();
            if (DsShapeViewModel.GeometryEditingMode) CreateControlPoints();
        }

        /// <summary>
        ///     Closes the figure just built by joining its last point back to its first with a middle
        ///     handle, so the closing segment can be moved like any other.
        /// </summary>
        private void ControlPointsFinalizeFigure()
        {
            PathControlPoint? figureStartPoint = null;
            PathControlPoint? figureEndPoint = null;
            var figurePathControlPointsCount = 0;
            foreach (ControlPoint controlPoint in _controlPoints.Reverse<ControlPoint>())
            {
                var pathControlPoint = controlPoint as PathControlPoint;
                if (pathControlPoint is not null)
                {
                    figurePathControlPointsCount += 1;
                    if (figureEndPoint is null) figureEndPoint = pathControlPoint;
                    if (pathControlPoint.Type == PathControlPointType.FigureStartPoint)
                    {
                        figureStartPoint = pathControlPoint;
                        break;
                    }
                }
            }

            if (figureStartPoint is not null && figureEndPoint is not null && figurePathControlPointsCount > 2)
                ControlPointsAdd(new MiddleControlPoint(this, figureEndPoint, figureStartPoint));
        }

        #endregion

        #region private fields

        private readonly List<ControlPoint> _controlPoints = new();

        private readonly Path _controlPointsPath;

        private readonly Path _selectedControlPointPath;

        #endregion
    }
}
