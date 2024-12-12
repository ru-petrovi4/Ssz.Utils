using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ssz.Operator.Core.ControlsDesign.GeometryEditing;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DesignGeometryDsShapeView : DesignDsShapeView
    {
        #region protected functions

        protected virtual void CreateControlPoints()
        {
            var pathGeometry = GeometryDsShapeView.Geometry as PathGeometry;
            if (pathGeometry is null) return;

            PathControlPoint? lastPathControlPoint = null;

            pathGeometry.DoForAllPointDependencyProperties((obj, dp) =>
            {
                if (dp.PropertyType == typeof(Point))
                {
                    var pathControlPoint = new PathControlPoint(this, obj, dp, 0);
                    if (pathControlPoint.Type == PathControlPointType.FigureStartPoint && _controlPoints.Count > 0)
                        ControlPointsFinalizeFigure();
                    ControlPointsAdd(pathControlPoint);

                    if (lastPathControlPoint is not null)
                        ControlPointsAdd(new MiddleControlPoint(this, lastPathControlPoint, pathControlPoint));

                    lastPathControlPoint = pathControlPoint;
                }
                else if (dp.PropertyType == typeof(PointCollection))
                {
                    var pointCollection = (PointCollection) obj.GetValue(dp);
                    for (var index = 0; index < pointCollection.Count; index += 1)
                    {
                        var point = pointCollection[index];
                        var pathControlPoint = new PathControlPoint(this, obj, dp, index);
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
                Data = ControlPointsGeometryGroup
            };
            Panel.SetZIndex(_controlPointsPath, 1);

            SelectedControlPointGeometryGroup = new GeometryGroup();
            _selectedControlPointPath = new Path
            {
                Stroke = new SolidColorBrush(Colors.Blue),
                StrokeThickness = 1.0,
                Fill = new SolidColorBrush(Colors.Blue),
                Opacity = 0.5,
                Data = SelectedControlPointGeometryGroup
            };
            Panel.SetZIndex(_selectedControlPointPath, 2);

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

        public static readonly DependencyProperty SelectedControlPointProperty = DependencyProperty.Register(
            "SelectedControlPoint", typeof(ControlPoint), typeof(DesignDrawingCanvas), new PropertyMetadata(null));

        public ControlPoint? SelectedControlPoint
        {
            get => (ControlPoint?) GetValue(SelectedControlPointProperty);
            set
            {
                var selectedControlPoint = (ControlPoint?) GetValue(SelectedControlPointProperty);
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

        public IEnumerable<ControlPoint> ControlPointsOrdered
        {
            get { return _controlPoints.OrderByDescending(cp => cp.ZIndex).ThenByDescending(cp => cp.Num); }
        }


        public GeometryGroup ControlPointsGeometryGroup { get; }


        public GeometryGroup SelectedControlPointGeometryGroup { get; }

        public bool HitTestPath(Point dsShapePoint)
        {
            _hitTestPathResult = false;

            VisualTreeHelper.HitTest((Visual) GeometryDsShapeView, null,
                HitTest,
                new PointHitTestParameters(dsShapePoint));

            return _hitTestPathResult;
        }


        public void AddPoint(PathControlPoint pathControlPoint)
        {
            var pathGeometry = GeometryDsShapeView.Geometry as PathGeometry;
            if (pathGeometry is null) return;

            switch (pathControlPoint.Type)
            {
                case PathControlPointType.FigureStartPoint:
                {
                    var newPathSegment = GetNewPathSegment((Point) pathControlPoint.Obj.GetValue(pathControlPoint.Dp));

                    if (newPathSegment is not null)
                    {
                        ClearControlPoints();
                        ((PathFigure) pathControlPoint.Obj).Segments.Insert(0, newPathSegment);
                        GeometryDsShapeView.UpdateModelLayer();
                    }
                }
                    break;
                case PathControlPointType.SegmentInPointsCollection:
                {
                    var pointCollection = (PointCollection) pathControlPoint.Obj.GetValue(pathControlPoint.Dp);
                    var point = pointCollection[pathControlPoint.Index];

                    ClearControlPoints();
                    pointCollection.Insert(pathControlPoint.Index, point);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentOtherPoint:
                {
                    var pathSegment = (PathSegment) pathControlPoint.Obj;

                    foreach (PathFigure f in pathGeometry.Figures)
                    {
                        var i = f.Segments.IndexOf(pathSegment);
                        if (i > -1)
                        {
                            var newPathSegment = GetNewPathSegment(pathSegment.GetSegmentEndPoint());

                            if (newPathSegment is not null)
                            {
                                ClearControlPoints();
                                f.Segments.Insert(i + 1, newPathSegment);
                                GeometryDsShapeView.UpdateModelLayer();
                            }

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
                    pathGeometry.Figures.Remove(pathFigure);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentInPointsCollection:
                {
                    var pointCollection = (PointCollection) pathControlPoint.Obj.GetValue(pathControlPoint.Dp);

                    ClearControlPoints();
                    pointCollection.RemoveAt(pathControlPoint.Index);
                    GeometryDsShapeView.UpdateModelLayer();
                }
                    break;
                case PathControlPointType.SegmentOtherPoint:
                {
                    var pathSegment = (PathSegment) pathControlPoint.Obj;
                    PathFigure? pathFigure = null;
                    foreach (PathFigure f in pathGeometry.Figures)
                        if (f.Segments.Contains(pathSegment))
                        {
                            pathFigure = f;
                            break;
                        }

                    if (pathFigure is not null)
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

        #region private functions

        private PathSegment GetNewPathSegment(Point point)
        {
            return new LineSegment(point, true);
        }

        private void OnGeometryChanged()
        {
            ClearControlPoints();
            if (DsShapeViewModel.GeometryEditingMode) CreateControlPoints();
        }

        private HitTestResultBehavior HitTest(HitTestResult result)
        {
            if (result.VisualHit is Path)
            {
                _hitTestPathResult = true;
                return HitTestResultBehavior.Stop;
            }

            return HitTestResultBehavior.Continue;
        }

        private void ControlPointsFinalizeFigure()
        {
            PathControlPoint? figureStartPoint = null;
            PathControlPoint? figureEndPoint = null;
            var figurePathControlPointsCount = 0;
            foreach (var controlPoint in _controlPoints.Reverse<ControlPoint>())
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

        private bool _hitTestPathResult;

        #endregion
    }
}

/*
        public bool HitTestPath(Point point)
        {
            var path = GeometryDsShapeView.Path;
            if (path.Stroke is not null)
            {
                double thickness = path.StrokeThickness;
                if (path.Fill is null)
                {
                    thickness = Math.Max(5.0, thickness);
                }
                if (path.Data.StrokeContains(new Pen(new SolidColorBrush(Colors.Black), thickness), point))
                {
                    return true;
                }
            }

            if (path.Fill is not null)
            {
                if (path.Data.FillContains(point))
                {
                    return true;
                }
            }

            return false;
        }

        public void AddSegment(object? sender, ExecutedRoutedEventArgs e)
        {            
            var newSegment = new LineSegment();
            if (relativeTo is PathFigure)
            {
                var figure = relativeTo as PathFigure;
                if (after)
                {
                    newSegment.Point = figure.StartPoint +
                                       (GetSegmentEndPoint(figure, figure.Segments[0]) - figure.StartPoint) * 0.5;
                    figure.Segments.Insert(0, newSegment);
                }
                else
                {
                    Point ept = GetSegmentEndPoint(figure, figure.Segments[figure.Segments.Count - 1]);
                    newSegment.Point = ept + (figure.StartPoint - ept) * 0.5;
                    figure.Segments.Add(newSegment);
                }
            }
            else if (relativeTo is PathSegment)
            {
                var segment = relativeTo as PathSegment;
                PathFigure? figure = null;
                foreach (PathFigure f in PathGeometry.Figures)
                {
                    if (f.Segments.Contains(segment))
                    {
                        figure = f;
                    }
                }
                if (figure is null)
                {
                    throw new ArgumentException("Segment of controlPoint is not in this path");
                }

                if (after)
                {
                    Point p1 = GetSegmentEndPoint(figure, segment);

                    Point p2;
                    if (figure.Segments.IndexOf(segment) == figure.Segments.Count - 1)
                    {
                        p2 = figure.StartPoint;
                    }
                    else
                    {
                        p2 = GetSegmentEndPoint(figure, figure.Segments[figure.Segments.IndexOf(segment) + 1]);
                    }
                    newSegment.Point = p1 + (p2 - p1) * 0.5;
                    figure.Segments.Insert(figure.Segments.IndexOf(segment) + 1, newSegment);
                }
                else
                {
                    Point p1 = GetSegmentStartPoint(figure, segment);
                    Point p2 = GetSegmentEndPoint(figure, segment);
                    newSegment.Point = p1 + (p2 - p1) * 0.5;
                    figure.Segments.Insert(figure.Segments.IndexOf(segment), newSegment);
                }
            }

            ClearControlPoints();
            return CreateControlPointsInternal(newSegment);
        }

        public void ChangeSegmentType(object? sender, ExecutedRoutedEventArgs e)
        {
            /*
            PathFigure? figure = null;
            foreach (PathFigure f in PathGeometry.Figures)
            {
                if (f.Segments.Contains(controlPoint.Tag as PathSegment))
                {
                    figure = f;
                }
            }
            if (figure is null)
            {
                throw new ArgumentException("Segment of controlPoint is not in this path");
            }

            Point startPoint = GetSegmentStartPoint(figure, controlPoint.Tag as PathSegment);
            Point endPoint = GetSegmentEndPoint(figure, controlPoint.Tag as PathSegment);
            var pathControlPoint1 = new Point();
            var pathControlPoint2 = new Point();

            if (controlPoint.Tag is LineSegment || controlPoint.Tag is ArcSegment)
            {
                pathControlPoint1 = startPoint + (endPoint - startPoint) * 0.3;
                pathControlPoint2 = endPoint + (startPoint - endPoint) * 0.3;
            }
            else if (controlPoint.Tag is BezierSegment)
            {
                pathControlPoint1 = (controlPoint.Tag as BezierSegment).Point1;
                pathControlPoint2 = (controlPoint.Tag as BezierSegment).Point2;
            }
            else if (controlPoint.Tag is QuadraticBezierSegment)
            {
                pathControlPoint1 = startPoint + (endPoint - startPoint) * 0.3;
                pathControlPoint2 = (controlPoint.Tag as QuadraticBezierSegment).Point1;
            }

            PathSegment? newSegment = null;
            if (type == @"LineSegment")
            {
                var lSegment = new LineSegment();
                lSegment.Point = endPoint;
                newSegment = lSegment;
            }
            else if (type == @"ArcSegment")
            {
                var aSegment = new ArcSegment();
                aSegment.Point = endPoint;
                Vector d = endPoint - startPoint;
                aSegment.Size = new Size(Math.Abs(d.X) * 2.0, Math.Abs(d.Y) * 2.0);
                aSegment.IsLargeArc = false;
                aSegment.SweepDirection = SweepDirection.Clockwise;
                newSegment = aSegment;
            }
            else if (type == @"BezierSegment")
            {
                var bSegment = new BezierSegment();
                bSegment.Point1 = pathControlPoint1;
                bSegment.Point2 = pathControlPoint2;
                bSegment.Point3 = endPoint;
                newSegment = bSegment;
            }
            else if (type == @"QuadraticBezierSegment")
            {
                var qSegment = new QuadraticBezierSegment();
                qSegment.Point1 = pathControlPoint2;
                qSegment.Point2 = endPoint;
                newSegment = qSegment;
            }

            if (newSegment is not null)
            {
                figure.Segments.Insert(figure.Segments.IndexOf(controlPoint.Tag as PathSegment), newSegment);
                figure.Segments.Remove(controlPoint.Tag as PathSegment);
                ClearControlPoints();
                controlPoint = CreateControlPointsInternal(newSegment);
            }
            return controlPoint;
        }
     
    private Point GetSegmentStartPoint(PathFigure figure, PathSegment pathSegment)
        {
            int n = figure.Segments.IndexOf(pathSegment) - 1;
            if (n < 0)
            {
                return figure.StartPoint;
            }
            return GetSegmentEndPoint(figure, figure.Segments[n]);
        }        

    private static void SetSegmentPoint(PathSegment segment, string propName, string str)
        {
            if (str is not null)
            {
                PropertyInfo pi = segment.GetType().GetProperty(propName);
                if (pi is not null)
                {                    
                    string[] props = str.Split(',', ' ');
                    pi.SetValue(segment, new Point(double.Parse(props[0], NumberStyles.Any, CultureInfo.InvariantCulture), double.Parse(props[1], NumberStyles.Any, CultureInfo.InvariantCulture)), null);
                }
            }
        }

    var newSegment = new LineSegment();
            if (relativeTo is PathFigure)
            {
                var figure = relativeTo as PathFigure;
                if (after)
                {
                    newSegment.Point = figure.StartPoint +
                                       (GetSegmentEndPoint(figure, figure.Segments[0]) - figure.StartPoint) * 0.5;
                    figure.Segments.Insert(0, newSegment);
                }
                else
                {
                    Point ept = GetSegmentEndPoint(figure, figure.Segments[figure.Segments.Count - 1]);
                    newSegment.Point = ept + (figure.StartPoint - ept) * 0.5;
                    figure.Segments.Add(newSegment);
                }
            }
            else if (relativeTo is PathSegment)
            {
                var segment = relativeTo as PathSegment;
                PathFigure? figure = null;
                foreach (PathFigure f in PathGeometry.Figures)
                {
                    if (f.Segments.Contains(segment))
                    {
                        figure = f;
                    }
                }
                if (figure is null)
                {
                    throw new ArgumentException("Segment of controlPoint is not in this path");
                }

                if (after)
                {
                    Point p1 = GetSegmentEndPoint(figure, segment);

                    Point p2;
                    if (figure.Segments.IndexOf(segment) == figure.Segments.Count - 1)
                    {
                        p2 = figure.StartPoint;
                    }
                    else
                    {
                        p2 = GetSegmentEndPoint(figure, figure.Segments[figure.Segments.IndexOf(segment) + 1]);
                    }
                    newSegment.Point = p1 + (p2 - p1) * 0.5;
                    figure.Segments.Insert(figure.Segments.IndexOf(segment) + 1, newSegment);
                }
                else
                {
                    Point p1 = GetSegmentStartPoint(figure, segment);
                    Point p2 = GetSegmentEndPoint(figure, segment);
                    newSegment.Point = p1 + (p2 - p1) * 0.5;
                    figure.Segments.Insert(figure.Segments.IndexOf(segment), newSegment);
                }
            }
     
     */