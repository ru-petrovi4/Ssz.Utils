using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    public static class PathGeometryExtensions
    {
        #region public functions

        public static Point GetSegmentEndPoint(this PathSegment pathSegment)
        {
            var lineSegment = pathSegment as LineSegment;
            if (lineSegment is not null) return lineSegment.Point;
            var polyLineSegment = pathSegment as PolyLineSegment;
            if (polyLineSegment is not null) return polyLineSegment.Points.Last();
            var arcSegment = pathSegment as ArcSegment;
            if (arcSegment is not null) return arcSegment.Point;
            var bezierSegment = pathSegment as BezierSegment;
            if (bezierSegment is not null) return bezierSegment.Point3;
            var polyBezierSegment = pathSegment as PolyBezierSegment;
            if (polyBezierSegment is not null) return polyBezierSegment.Points.Last();
            var quadraticBezierSegment = pathSegment as QuadraticBezierSegment;
            if (quadraticBezierSegment is not null) return quadraticBezierSegment.Point2;
            var polyQuadraticBezierSegment = pathSegment as PolyQuadraticBezierSegment;
            if (polyQuadraticBezierSegment is not null) return polyQuadraticBezierSegment.Points.Last();
            return new Point();
        }


        public static void Normalize(this PathGeometry pathGeometry, double width, double height,
            double strokeThickness)
        {
            if (pathGeometry is null) return;

            var bounds = pathGeometry.Bounds;
            var preDeltaX = -bounds.X;
            var preDeltaY = -bounds.Y;
            double postDeltaX;
            double postDeltaY;
            double kX;
            if (bounds.Width > 0)
            {
                kX = (width - strokeThickness) / bounds.Width;
                postDeltaX = strokeThickness / 2;
            }
            else
            {
                kX = 1.0;
                postDeltaX = width / 2;
            }

            double kY;
            if (bounds.Height > 0)
            {
                kY = (height - strokeThickness) / bounds.Height;
                postDeltaY = strokeThickness / 2;
            }
            else
            {
                kY = 1.0;
                postDeltaY = height / 2;
            }

            pathGeometry.Transform(preDeltaX, preDeltaY, kX, kY, postDeltaX, postDeltaY);
        }


        public static void Transform(this PathGeometry pathGeometry, double preDeltaX, double preDeltaY, double kX,
            double kY,
            double postDeltaX, double postDeltaY)
        {
            if (pathGeometry is null) return;

            foreach (PathFigure pathFigure in pathGeometry.Figures)
            {
                var point = pathFigure.StartPoint;
                pathFigure.StartPoint = new Point((point.X + preDeltaX) * kX + postDeltaX,
                    (point.Y + preDeltaY) * kY + postDeltaY);
                foreach (PathSegment pathSegment in pathFigure.Segments)
                {
                    var arcSegment = pathSegment as ArcSegment;
                    if (arcSegment is not null)
                        arcSegment.Size = new Size(arcSegment.Size.Width * kY, arcSegment.Size.Height * kX);
                    foreach (FieldInfo fi in pathSegment.GetType().GetFields().OrderBy(i => i.Name))
                    {
                        if (fi.FieldType != typeof(DependencyProperty)) continue;
                        var dp = fi.GetValue(null) as DependencyProperty;
                        if (dp is null) continue;
                        if (dp.PropertyType == typeof(Point))
                        {
                            point = (Point) pathSegment.GetValue(dp);
                            pathSegment.SetValue(dp,
                                new Point((point.X + preDeltaX) * kX + postDeltaX,
                                    (point.Y + preDeltaY) * kY + postDeltaY));
                        }
                        else if (dp.PropertyType == typeof(PointCollection))
                        {
                            var pointCollection = (PointCollection) pathSegment.GetValue(dp);
                            for (var i = 0; i < pointCollection.Count; i += 1)
                            {
                                point = pointCollection[i];
                                pointCollection[i] = new Point((point.X + preDeltaX) * kX + postDeltaX,
                                    (point.Y + preDeltaY) * kY + postDeltaY);
                            }
                        }
                    }
                }
            }
        }

        public static void DoForAllPoints(this PathGeometry pathGeometry, RefPointCallback callback)
        {
            if (pathGeometry is null) return;

            foreach (PathFigure pathFigure in pathGeometry.Figures)
            {
                var point = pathFigure.StartPoint;
                callback(ref point);
                pathFigure.StartPoint = point;
                foreach (PathSegment pathSegment in pathFigure.Segments)
                foreach (FieldInfo fi in pathSegment.GetType().GetFields().OrderBy(i => i.Name))
                {
                    if (fi.FieldType != typeof(DependencyProperty)) continue;
                    var dp = fi.GetValue(null) as DependencyProperty;
                    if (dp is null) continue;
                    if (dp.PropertyType == typeof(Point))
                    {
                        point = (Point) pathSegment.GetValue(dp);
                        callback(ref point);
                        pathSegment.SetValue(dp, point);
                    }
                    else if (dp.PropertyType == typeof(PointCollection))
                    {
                        var pointCollection = (PointCollection) pathSegment.GetValue(dp);
                        for (var i = 0; i < pointCollection.Count; i += 1)
                        {
                            point = pointCollection[i];
                            callback(ref point);
                            pointCollection[i] = point;
                        }
                    }
                }
            }
        }

        public static void DoForAllPointDependencyProperties(this PathGeometry pathGeometry,
            DependencyPropertyCallback callback)
        {
            if (pathGeometry is null) return;

            foreach (PathFigure pathFigure in pathGeometry.Figures)
            {
                Type pathFigureType = pathFigure.GetType();
                var startPointDp = pathFigureType.GetField(@"StartPointProperty")?.GetValue(null) as DependencyProperty;
                if (startPointDp is not null) callback(pathFigure, startPointDp);
                foreach (PathSegment pathSegment in pathFigure.Segments)
                foreach (FieldInfo fi in pathSegment.GetType().GetFields().OrderBy(i => i.Name))
                {
                    if (fi.FieldType != typeof(DependencyProperty)) continue;
                    var dp = fi.GetValue(null) as DependencyProperty;
                    if (dp is null) continue;
                    if (dp.PropertyType == typeof(Point))
                        callback(pathSegment, dp);
                    else if (dp.PropertyType == typeof(PointCollection)) callback(pathSegment, dp);
                }
            }
        }

        #endregion
    }

    public delegate void RefPointCallback(ref Point pt);

    public delegate void DependencyPropertyCallback(DependencyObject obj, DependencyProperty dp);
}

/*
public static Point GetFirstStartPoint(this PathGeometry pathGeometry)
        {
            if (pathGeometry is null || pathGeometry.Figures.Count == 0) return new Point();

            return pathGeometry.Figures[0].StartPoint;
        }

        public static IEnumerable<Point> GetAllPoints(this PathGeometry pathGeometry)
        {
            if (pathGeometry is not null)
                foreach (PathFigure pathFigure in pathGeometry.Figures)
                {
                    yield return pathFigure.StartPoint;
                    foreach (PathSegment pathSegment in pathFigure.Segments)
                    {
                        foreach (FieldInfo fi in pathSegment.GetType().GetFields())
                        {
                            if (fi.FieldType == typeof (DependencyProperty))
                            {
                                var dp = fi.GetValue(null) as DependencyProperty;
                                if (dp.PropertyType == typeof (Point))
                                {
                                    yield return (Point) pathSegment.GetValue(dp);
                                }
                            }
                        }
                    }
                }
        }
        
 */