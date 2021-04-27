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
    internal struct Segment
    {
        #region Constructors

        public Segment(Point point)
        {
            _p1 = point;
            _p2 = point;
            IsP1Excluded = false;
            IsP2Excluded = false;
        }

        public Segment(Point p1, Point p2)
        {
            _p1 = p1;
            _p2 = p2;
            IsP1Excluded = false;
            IsP2Excluded = false;
        }

        public Segment(Point p1, Point p2, bool excludeP1, bool excludeP2)
        {
            _p1 = p1;
            _p2 = p2;
            IsP1Excluded = excludeP1;
            IsP2Excluded = excludeP2;
        }

        #endregion

        #region Empty Static Properties

        public static Segment Empty
        {
            get
            {
                var result = new Segment(new Point(0, 0));
                result.IsP1Excluded = true;
                result.IsP2Excluded = true;
                return result;
            }
        }

        #endregion

        #region P1 Property

        public Point P1 => _p1;

        #endregion

        #region P2 Property

        public Point P2 => _p2;

        #endregion

        #region IsP1Excluded Property

        public bool IsP1Excluded { get; private set; }

        #endregion

        #region IsP2Excluded Property

        public bool IsP2Excluded { get; private set; }

        #endregion

        #region IsEmpty Property

        public bool IsEmpty => DoubleHelper.AreVirtuallyEqual(_p1, _p2) && (IsP1Excluded || IsP2Excluded);

        #endregion

        #region IsPoint Property

        public bool IsPoint => DoubleHelper.AreVirtuallyEqual(_p1, _p2);

        #endregion

        #region Length Property

        public double Length => (P2 - P1).Length;

        #endregion

        #region Slope Property

        public double Slope => P2.X == P1.X ? double.NaN : (P2.Y - P1.Y) / (P2.X - P1.X);

        #endregion

        public bool Contains(Point point)
        {
            if (IsEmpty)
                return false;

            // if the point is an endpoint, ensure that it is not excluded
            if (DoubleHelper.AreVirtuallyEqual(_p1, point))
                return IsP1Excluded;

            if (DoubleHelper.AreVirtuallyEqual(_p2, point))
                return IsP2Excluded;

            var result = false;

            // ensure that a line through P1 and the point is parallel to the current segment
            if (DoubleHelper.AreVirtuallyEqual(Slope, new Segment(_p1, point).Slope))
                // finally, ensure that the point is between the segment's endpoints
                result = point.X >= Math.Min(_p1.X, _p2.X)
                         && point.X <= Math.Max(_p1.X, _p2.X)
                         && point.Y >= Math.Min(_p1.Y, _p2.Y)
                         && point.Y <= Math.Max(_p1.Y, _p2.Y);
            return result;
        }

        public bool Contains(Segment segment)
        {
            return segment == Intersection(segment);
        }

        public override bool Equals(object o)
        {
            if (!(o is Segment))
                return false;

            var other = (Segment) o;

            // empty segments are always considered equal
            if (IsEmpty)
                return other.IsEmpty;

            // segments are considered equal if
            //    1) the endpoints are equal and equally excluded
            //    2) the opposing endpoints are equal and equally excluded
            if (DoubleHelper.AreVirtuallyEqual(_p1, other._p1))
                return DoubleHelper.AreVirtuallyEqual(_p2, other._p2)
                       && IsP1Excluded == other.IsP1Excluded
                       && IsP2Excluded == other.IsP2Excluded;
            return DoubleHelper.AreVirtuallyEqual(_p1, other._p2)
                   && DoubleHelper.AreVirtuallyEqual(_p2, other._p1)
                   && IsP1Excluded == other.IsP2Excluded
                   && IsP2Excluded == other.IsP1Excluded;
        }

        public override int GetHashCode()
        {
            return _p1.GetHashCode() ^ _p2.GetHashCode() ^ IsP1Excluded.GetHashCode() ^ IsP2Excluded.GetHashCode();
        }

        public Segment Intersection(Segment segment)
        {
            // if either segment is empty, the intersection is also empty
            if (IsEmpty || segment.IsEmpty)
                return Empty;

            // if the segments are equal, just return a new equal segment
            if (this == segment)
                return new Segment(_p1, _p2, IsP1Excluded, IsP2Excluded);

            // if either segment is a Point, just see if the point is contained in the other segment
            if (IsPoint)
                return segment.Contains(_p1) ? new Segment(_p1) : Empty;

            if (segment.IsPoint)
                return Contains(segment._p1) ? new Segment(segment._p1) : Empty;

            // okay, no easy answer, so let's do the math...
            var p1 = _p1;
            var v1 = _p2 - _p1;
            var p2 = segment._p1;
            var v2 = segment._p2 - segment._p1;
            var endpointVector = p2 - p1;

            var xProd = Vector.CrossProduct(v1, v2);

            // if segments are not parallel, then look for intersection on each segment
            if (!DoubleHelper.AreVirtuallyEqual(Slope, segment.Slope))
            {
                // check for intersection on other segment
                var s = Vector.CrossProduct(endpointVector, v1) / xProd;
                if (s < 0 || s > 1)
                    return Empty;

                // check for intersection on this segment
                s = Vector.CrossProduct(endpointVector, v2) / xProd;
                if (s < 0 || s > 1)
                    return Empty;

                // intersection of segments is a point
                return new Segment(p1 + s * v1);
            }

            // segments are parallel
            xProd = Vector.CrossProduct(endpointVector, v1);
            if (xProd * xProd > 1.0e-06 * v1.LengthSquared * endpointVector.LengthSquared)
                // segments do not intersect
                return Empty;

            // intersection is overlapping segment
            var result = new Segment();

            // to determine the overlapping segment, create reference segments where the endpoints are *not* excluded
            var refThis = new Segment(_p1, _p2);
            var refSegment = new Segment(segment._p1, segment._p2);

            // check whether this segment is contained in the other segment
            var includeThisP1 = refSegment.Contains(refThis._p1);
            var includeThisP2 = refSegment.Contains(refThis._p2);
            if (includeThisP1 && includeThisP2)
            {
                result._p1 = _p1;
                result._p2 = _p2;
                result.IsP1Excluded = IsP1Excluded || !segment.Contains(_p1);
                result.IsP2Excluded = IsP2Excluded || !segment.Contains(_p2);
                return result;
            }

            // check whether the other segment is contained in this segment
            var includeSegmentP1 = refThis.Contains(refSegment._p1);
            var includeSegmentP2 = refThis.Contains(refSegment._p2);
            if (includeSegmentP1 && includeSegmentP2)
            {
                result._p1 = segment._p1;
                result._p2 = segment._p2;
                result.IsP1Excluded = segment.IsP1Excluded || !Contains(segment._p1);
                result.IsP2Excluded = segment.IsP2Excluded || !Contains(segment._p2);
                return result;
            }

            // the intersection must include one endpoint from this segment and one endpoint from the other segment
            if (includeThisP1)
            {
                result._p1 = _p1;
                result.IsP1Excluded = IsP1Excluded || !segment.Contains(_p1);
            }
            else
            {
                result._p1 = _p2;
                result.IsP1Excluded = IsP2Excluded || !segment.Contains(_p2);
            }

            if (includeSegmentP1)
            {
                result._p2 = segment._p1;
                result.IsP2Excluded = segment.IsP1Excluded || !Contains(segment._p1);
            }
            else
            {
                result._p2 = segment._p2;
                result.IsP2Excluded = segment.IsP2Excluded || !Contains(segment._p2);
            }

            return result;
        }

        public override string ToString()
        {
            var s = base.ToString();

            if (IsEmpty)
                s = s + ": {Empty}";
            else if (IsPoint)
                s = s + ", Point: " + _p1;
            else
                s = s + ": " + _p1 + (IsP1Excluded ? " (excl)" : " (incl)")
                    + " to " + _p2 + (IsP2Excluded ? " (excl)" : " (incl)");

            return s;
        }

        #region Operators Methods

        public static bool operator ==(Segment s1, Segment s2)
        {
            if ((object) s1 == null)
                return (object) s2 == null;

            if ((object) s2 == null)
                return (object) s1 == null;

            return s1.Equals(s2);
        }

        public static bool operator !=(Segment s1, Segment s2)
        {
            return !(s1 == s2);
        }

        #endregion

        #region Private Fields

        private Point _p1;
        private Point _p2;

        #endregion
    }
}