using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     Reads and moves the points a path is made of.
    ///     <para>
    ///         Ported from the WPF editor. The points still come from the properties of the segments by
    ///         reflection, because a path segment carries them under different names and this way each
    ///         new kind of segment is handled without naming it here; the property type is Avalonia's
    ///         instead of WPF's, and a list of points is IList&lt;Point&gt; where WPF had a
    ///         PointCollection.
    ///     </para>
    ///     <para>
    ///         Avalonia has no poly bezier segments, so unlike the WPF version there is nothing to
    ///         handle for them.
    ///     </para>
    /// </summary>
    public static class PathGeometryExtensions
    {
        #region public functions

        public static Point GetSegmentEndPoint(this PathSegment pathSegment)
        {
            var lineSegment = pathSegment as LineSegment;
            if (lineSegment is not null) return lineSegment.Point;
            var polyLineSegment = pathSegment as PolyLineSegment;
            if (polyLineSegment is not null && polyLineSegment.Points is not null &&
                polyLineSegment.Points.Count > 0) return polyLineSegment.Points.Last();
            var arcSegment = pathSegment as ArcSegment;
            if (arcSegment is not null) return arcSegment.Point;
            var bezierSegment = pathSegment as BezierSegment;
            if (bezierSegment is not null) return bezierSegment.Point3;
            var quadraticBezierSegment = pathSegment as QuadraticBezierSegment;
            if (quadraticBezierSegment is not null) return quadraticBezierSegment.Point2;
            return new Point();
        }

        /// <summary>
        ///     Fits the path into the given box, leaving room for half the stroke on each side, which is
        ///     what makes a shape drawn from a path fill its own bounds exactly.
        /// </summary>
        public static void Normalize(this PathGeometry pathGeometry, double width, double height,
            double strokeThickness)
        {
            if (pathGeometry is null) return;

            Rect bounds = pathGeometry.Bounds;
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

            foreach (PathFigure pathFigure in pathGeometry.Figures ?? Enumerable.Empty<PathFigure>())
            {
                Point point = pathFigure.StartPoint;
                pathFigure.StartPoint = new Point((point.X + preDeltaX) * kX + postDeltaX,
                    (point.Y + preDeltaY) * kY + postDeltaY);
                foreach (PathSegment pathSegment in pathFigure.Segments ?? Enumerable.Empty<PathSegment>())
                {
                    var arcSegment = pathSegment as ArcSegment;
                    if (arcSegment is not null)
                        arcSegment.Size = new Size(arcSegment.Size.Width * kY, arcSegment.Size.Height * kX);

                    foreach (AvaloniaProperty avaloniaProperty in GetPointProperties(pathSegment.GetType()))
                    {
                        if (avaloniaProperty.PropertyType == typeof(Point))
                        {
                            point = (Point) pathSegment.GetValue(avaloniaProperty)!;
                            pathSegment.SetValue(avaloniaProperty,
                                new Point((point.X + preDeltaX) * kX + postDeltaX,
                                    (point.Y + preDeltaY) * kY + postDeltaY));
                        }
                        else
                        {
                            var points = pathSegment.GetValue(avaloniaProperty) as IList<Point>;
                            if (points is null) continue;
                            for (var i = 0; i < points.Count; i += 1)
                            {
                                point = points[i];
                                points[i] = new Point((point.X + preDeltaX) * kX + postDeltaX,
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

            foreach (PathFigure pathFigure in pathGeometry.Figures ?? Enumerable.Empty<PathFigure>())
            {
                Point point = pathFigure.StartPoint;
                callback(ref point);
                pathFigure.StartPoint = point;
                foreach (PathSegment pathSegment in pathFigure.Segments ?? Enumerable.Empty<PathSegment>())
                foreach (AvaloniaProperty avaloniaProperty in GetPointProperties(pathSegment.GetType()))
                {
                    if (avaloniaProperty.PropertyType == typeof(Point))
                    {
                        point = (Point) pathSegment.GetValue(avaloniaProperty)!;
                        callback(ref point);
                        pathSegment.SetValue(avaloniaProperty, point);
                    }
                    else
                    {
                        var points = pathSegment.GetValue(avaloniaProperty) as IList<Point>;
                        if (points is null) continue;
                        for (var i = 0; i < points.Count; i += 1)
                        {
                            point = points[i];
                            callback(ref point);
                            points[i] = point;
                        }
                    }
                }
            }
        }

        public static void DoForAllPointProperties(this PathGeometry pathGeometry,
            AvaloniaPropertyCallback callback)
        {
            if (pathGeometry is null) return;

            foreach (PathFigure pathFigure in pathGeometry.Figures ?? Enumerable.Empty<PathFigure>())
            {
                callback(pathFigure, PathFigure.StartPointProperty);
                foreach (PathSegment pathSegment in pathFigure.Segments ?? Enumerable.Empty<PathSegment>())
                foreach (AvaloniaProperty avaloniaProperty in GetPointProperties(pathSegment.GetType()))
                    callback(pathSegment, avaloniaProperty);
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     The properties of a segment type that hold a point or a list of points, in the order their
        ///     names give - which is the order they appear along the segment.
        /// </summary>
        private static AvaloniaProperty[] GetPointProperties(Type pathSegmentType)
        {
            lock (PointPropertiesCache)
            {
                if (PointPropertiesCache.TryGetValue(pathSegmentType, out AvaloniaProperty[]? cached))
                    return cached;

                var pointProperties = new List<AvaloniaProperty>();
                foreach (FieldInfo fieldInfo in pathSegmentType.GetFields(BindingFlags.Public | BindingFlags.Static)
                             .OrderBy(i => i.Name))
                {
                    if (!typeof(AvaloniaProperty).IsAssignableFrom(fieldInfo.FieldType)) continue;
                    var avaloniaProperty = fieldInfo.GetValue(null) as AvaloniaProperty;
                    if (avaloniaProperty is null) continue;
                    if (avaloniaProperty.PropertyType == typeof(Point) ||
                        typeof(IList<Point>).IsAssignableFrom(avaloniaProperty.PropertyType))
                        pointProperties.Add(avaloniaProperty);
                }

                AvaloniaProperty[] result = pointProperties.ToArray();
                PointPropertiesCache[pathSegmentType] = result;
                return result;
            }
        }

        #endregion

        #region private fields

        private static readonly Dictionary<Type, AvaloniaProperty[]> PointPropertiesCache = new();

        #endregion
    }

    public delegate void RefPointCallback(ref Point pt);

    public delegate void AvaloniaPropertyCallback(AvaloniaObject obj, AvaloniaProperty avaloniaProperty);
}
