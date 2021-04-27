/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.Core.Utilities
{
    internal static class RectHelper
    {
        public static Point Center(Rect rect)
        {
            return new(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
        }

        public static Point? GetNearestPointOfIntersectionBetweenRectAndSegment(Rect rect, Segment segment, Point point)
        {
            Point? result = null;
            var distance = double.PositiveInfinity;

            var leftIntersection = segment.Intersection(new Segment(rect.BottomLeft, rect.TopLeft));
            var topIntersection = segment.Intersection(new Segment(rect.TopLeft, rect.TopRight));
            var rightIntersection = segment.Intersection(new Segment(rect.TopRight, rect.BottomRight));
            var bottomIntersection = segment.Intersection(new Segment(rect.BottomRight, rect.BottomLeft));

            AdjustResultForIntersectionWithSide(ref result, ref distance, leftIntersection, point);
            AdjustResultForIntersectionWithSide(ref result, ref distance, topIntersection, point);
            AdjustResultForIntersectionWithSide(ref result, ref distance, rightIntersection, point);
            AdjustResultForIntersectionWithSide(ref result, ref distance, bottomIntersection, point);

            return result;
        }

        public static Rect GetRectCenteredOnPoint(Point center, Size size)
        {
            return new(new Point(center.X - size.Width / 2, center.Y - size.Height / 2), size);
        }

        private static void AdjustResultForIntersectionWithSide(ref Point? result, ref double distance,
            Segment intersection, Point point)
        {
            if (!intersection.IsEmpty)
            {
                if (intersection.Contains(point))
                {
                    distance = 0;
                    result = point;
                    return;
                }

                var p1Distance = PointHelper.DistanceBetween(point, intersection.P1);
                var p2Distance = double.PositiveInfinity;
                if (!intersection.IsPoint) p2Distance = PointHelper.DistanceBetween(point, intersection.P2);

                if (Math.Min(p1Distance, p2Distance) < distance)
                {
                    if (p1Distance < p2Distance)
                    {
                        distance = p1Distance;
                        result = intersection.P1;
                    }
                    else
                    {
                        distance = p2Distance;
                        result = intersection.P2;
                    }
                }
            }
        }
    }
}