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
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;
using Ssz.Xceed.Wpf.Toolkit.Core;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;
using Ssz.Xceed.Wpf.Toolkit.Primitives;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public sealed class Pie : ShapeBase
    {
        #region GeometryTransform Property

        public override Transform GeometryTransform => Transform.Identity;

        #endregion

        #region RenderedGeometry Property

        public override Geometry RenderedGeometry =>
            // for a Pie, the RenderedGeometry is the same as the DefiningGeometry
            DefiningGeometry;

        #endregion

        #region DefiningGeometry Protected Property

        protected override Geometry DefiningGeometry
        {
            get
            {
                var slice = Slice;
                if (_rect.IsEmpty || slice <= 0)
                    return Geometry.Empty;

                if (slice >= 1)
                    return new EllipseGeometry(_rect);

                // direction of flow is determined by the SweepDirection property
                var directionalFactor = SweepDirection == SweepDirection.Clockwise ? 1.0 : -1.0;
                var startAngle = StartAngle;

                var pointA = EllipseHelper.PointOfRadialIntersection(_rect, startAngle);
                var pointB =
                    EllipseHelper.PointOfRadialIntersection(_rect, startAngle + directionalFactor * slice * 360);
                var segments = new PathSegmentCollection();
                segments.Add(new LineSegment(pointA, true));
                var arc = new ArcSegment();
                arc.Point = pointB;
                arc.Size = new Size(_rect.Width / 2, _rect.Height / 2);
                arc.IsLargeArc = slice > 0.5;
                arc.SweepDirection = SweepDirection;
                segments.Add(arc);
                var figures = new PathFigureCollection();
                figures.Add(new PathFigure(RectHelper.Center(_rect), segments, true));
                return new PathGeometry(figures);
            }
        }

        #endregion

        #region IsUpdatingEndAngle Private Property

        private bool IsUpdatingEndAngle
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingEndAngle];
            set => _cacheBits[(int) CacheBits.IsUpdatingEndAngle] = value;
        }

        #endregion

        #region IsUpdatingMode Private Property

        private bool IsUpdatingMode
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingMode];
            set => _cacheBits[(int) CacheBits.IsUpdatingMode] = value;
        }

        #endregion

        #region IsUpdatingSlice Private Property

        private bool IsUpdatingSlice
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingSlice];
            set => _cacheBits[(int) CacheBits.IsUpdatingSlice] = value;
        }

        #endregion

        #region IsUpdatingStartAngle Private Property

        private bool IsUpdatingStartAngle
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingStartAngle];
            set => _cacheBits[(int) CacheBits.IsUpdatingStartAngle] = value;
        }

        #endregion

        #region IsUpdatingSweepDirection Private Property

        private bool IsUpdatingSweepDirection
        {
            get => _cacheBits[(int) CacheBits.IsUpdatingSweepDirection];
            set => _cacheBits[(int) CacheBits.IsUpdatingSweepDirection] = value;
        }

        #endregion

        internal override Size GetNaturalSize()
        {
            var strokeThickness = GetStrokeThickness();
            return new Size(strokeThickness, strokeThickness);
        }

        internal override Rect GetDefiningGeometryBounds()
        {
            return _rect;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var penThickness = GetStrokeThickness();
            var margin = penThickness / 2;

            _rect = new Rect(margin, margin,
                Math.Max(0, finalSize.Width - penThickness),
                Math.Max(0, finalSize.Height - penThickness));

            switch (Stretch)
            {
                case Stretch.None:
                    // empty rectangle
                    _rect.Width = _rect.Height = 0;
                    break;

                case Stretch.Fill:
                    // already initialized for Fill
                    break;

                case Stretch.Uniform:
                    // largest square that fits in the final size
                    if (_rect.Width > _rect.Height)
                        _rect.Width = _rect.Height;
                    else
                        _rect.Height = _rect.Width;
                    break;

                case Stretch.UniformToFill:

                    // smallest square that fills the final size
                    if (_rect.Width < _rect.Height)
                        _rect.Width = _rect.Height;
                    else
                        _rect.Height = _rect.Width;
                    break;
            }

            return finalSize;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (Stretch == Stretch.UniformToFill)
            {
                var width = constraint.Width;
                var height = constraint.Height;

                if (double.IsInfinity(width) && double.IsInfinity(height))
                    return GetNaturalSize();
                if (double.IsInfinity(width) || double.IsInfinity(height))
                    width = Math.Min(width, height);
                else
                    width = Math.Max(width, height);

                return new Size(width, width);
            }

            return GetNaturalSize();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!_rect.IsEmpty)
            {
                var pen = GetPen();
                drawingContext.DrawGeometry(Fill, pen, RenderedGeometry);
            }
        }

        #region CacheBits Nested Type

        private enum CacheBits
        {
            IsUpdatingEndAngle = 0x00000001,
            IsUpdatingMode = 0x00000002,
            IsUpdatingSlice = 0x00000004,
            IsUpdatingStartAngle = 0x00000008,
            IsUpdatingSweepDirection = 0x00000010
        }

        #endregion

        #region Constructors

        static Pie()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Pie), new FrameworkPropertyMetadata(typeof(Pie)));
            // The default stretch mode of Pie is Fill
            StretchProperty.OverrideMetadata(typeof(Pie), new FrameworkPropertyMetadata(Stretch.Fill));
            StrokeLineJoinProperty.OverrideMetadata(typeof(Pie), new FrameworkPropertyMetadata(PenLineJoin.Round));
        }

        #endregion

        #region EndAngle Property

        public static readonly DependencyProperty EndAngleProperty =
            DependencyProperty.Register("EndAngle", typeof(double), typeof(Pie),
                new FrameworkPropertyMetadata(360d,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnEndAngleChanged, CoerceEndAngleValue));

        public double EndAngle
        {
            get => (double) GetValue(EndAngleProperty);
            set => SetValue(EndAngleProperty, value);
        }

        private static void OnEndAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Pie) d).OnEndAngleChanged(e);
        }

        private void OnEndAngleChanged(DependencyPropertyChangedEventArgs e)
        {
            // avoid re-entrancy
            if (IsUpdatingEndAngle)
                return;

            if (!(IsUpdatingStartAngle || IsUpdatingSlice || IsUpdatingSweepDirection))
                switch (Mode)
                {
                    case PieMode.Slice:
                        throw new InvalidOperationException(
                            ErrorMessages.GetMessage("EndAngleCannotBeSetDirectlyInSlice"));
                }

            // EndAngle, Slice, and SweepDirection are interrelated and must be kept in sync
            IsUpdatingEndAngle = true;
            try
            {
                if (Mode == PieMode.EndAngle) CoerceValue(SweepDirectionProperty);
                CoerceValue(SliceProperty);
            }
            finally
            {
                IsUpdatingEndAngle = false;
            }
        }

        private static object CoerceEndAngleValue(DependencyObject d, object value)
        {
            // keep EndAngle in sync with Slice and SweepDirection
            var pie = (Pie) d;
            if (pie.IsUpdatingSlice || pie.IsUpdatingSweepDirection
                                    || pie.IsUpdatingStartAngle && pie.Mode == PieMode.Slice)
            {
                var newValue = pie.StartAngle +
                               (pie.SweepDirection == SweepDirection.Clockwise ? 1.0 : -1.0) * pie.Slice * 360;
                if (!DoubleHelper.AreVirtuallyEqual((double) value, newValue)) value = newValue;
            }

            return value;
        }

        #endregion

        #region Mode Property

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register("Mode", typeof(PieMode), typeof(Pie),
                new FrameworkPropertyMetadata(PieMode.Manual, OnModeChanged));

        public PieMode Mode
        {
            get => (PieMode) GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Pie) d).OnModeChanged(e);
        }

        private void OnModeChanged(DependencyPropertyChangedEventArgs e)
        {
            // disallow reentrancy
            if (IsUpdatingMode)
                return;

            IsUpdatingMode = true;
            try
            {
                if (Mode == PieMode.EndAngle) CoerceValue(SweepDirectionProperty);
            }
            finally
            {
                IsUpdatingMode = false;
            }
        }

        #endregion

        #region Slice Property

        public static readonly DependencyProperty SliceProperty =
            DependencyProperty.Register("Slice", typeof(double), typeof(Pie),
                new FrameworkPropertyMetadata(1d,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSliceChanged, CoerceSliceValue), ValidateSlice);

        public double Slice
        {
            get => (double) GetValue(SliceProperty);
            set => SetValue(SliceProperty, value);
        }

        private static void OnSliceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Pie) d).OnSliceChanged(e);
        }

        private void OnSliceChanged(DependencyPropertyChangedEventArgs e)
        {
            // avoid re-entrancy
            if (IsUpdatingSlice)
                return;

            if (!(IsUpdatingStartAngle || IsUpdatingEndAngle || IsUpdatingSweepDirection))
                if (Mode == PieMode.EndAngle)
                    throw new InvalidOperationException(ErrorMessages.GetMessage("SliceCannotBeSetDirectlyInEndAngle"));

            // EndAngle and Slice are interrelated and must be kept in sync
            IsUpdatingSlice = true;
            try
            {
                if (!(IsUpdatingStartAngle || IsUpdatingEndAngle || Mode == PieMode.Manual && IsUpdatingSweepDirection))
                    CoerceValue(EndAngleProperty);
            }
            finally
            {
                IsUpdatingSlice = false;
            }
        }

        private static object CoerceSliceValue(DependencyObject d, object value)
        {
            // keep Slice in sync with EndAngle, StartAngle, and SweepDirection
            var pie = (Pie) d;
            if (pie.IsUpdatingEndAngle || pie.IsUpdatingStartAngle || pie.IsUpdatingSweepDirection)
            {
                var slice = Math.Max(-360.0, Math.Min(360.0, pie.EndAngle - pie.StartAngle)) /
                            (pie.SweepDirection == SweepDirection.Clockwise ? 360.0 : -360.0);
                var newValue = DoubleHelper.AreVirtuallyEqual(slice, 0) ? 0 : slice < 0 ? slice + 1 : slice;
                if (!DoubleHelper.AreVirtuallyEqual((double) value, newValue)) value = newValue;
            }

            return value;
        }

        private static bool ValidateSlice(object value)
        {
            var newValue = (double) value;
            if (newValue < 0 || newValue > 1 || DoubleHelper.IsNaN(newValue))
                throw new ArgumentException(ErrorMessages.GetMessage("SliceOOR"));

            return true;
        }

        #endregion

        #region StartAngle Property

        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register("StartAngle", typeof(double), typeof(Pie),
                new FrameworkPropertyMetadata(360d,
                    FrameworkPropertyMetadataOptions.AffectsRender |
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnStartAngleChanged));

        public double StartAngle
        {
            get => (double) GetValue(StartAngleProperty);
            set => SetValue(StartAngleProperty, value);
        }

        private static void OnStartAngleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Pie) d).OnStartAngleChanged(e);
        }

        private void OnStartAngleChanged(DependencyPropertyChangedEventArgs e)
        {
            // avoid re-entrancy
            if (IsUpdatingStartAngle)
                return;

            // StartAngle, Slice, and SweepDirection are interrelated and must be kept in sync
            IsUpdatingStartAngle = true;
            try
            {
                switch (Mode)
                {
                    case PieMode.Manual:
                        CoerceValue(SliceProperty);
                        break;

                    case PieMode.EndAngle:
                        CoerceValue(SweepDirectionProperty);
                        CoerceValue(SliceProperty);
                        break;

                    case PieMode.Slice:
                        CoerceValue(EndAngleProperty);
                        break;
                }
            }
            finally
            {
                IsUpdatingStartAngle = false;
            }
        }

        #endregion

        #region SweepDirection Property

        public static readonly DependencyProperty SweepDirectionProperty =
            DependencyProperty.Register("SweepDirection", typeof(SweepDirection), typeof(Pie),
                new FrameworkPropertyMetadata(SweepDirection.Clockwise, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnSweepDirectionChanged, CoerceSweepDirectionValue));

        public SweepDirection SweepDirection
        {
            get => (SweepDirection) GetValue(SweepDirectionProperty);
            set => SetValue(SweepDirectionProperty, value);
        }

        private static void OnSweepDirectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Pie) d).OnSweepDirectionChanged(e);
        }

        private void OnSweepDirectionChanged(DependencyPropertyChangedEventArgs e)
        {
            // avoid re-entrancy
            if (IsUpdatingSweepDirection)
                return;

            // EndAngle, Slice, and SweepDirection are interrelated and must be kept in sync
            IsUpdatingSweepDirection = true;
            try
            {
                switch (Mode)
                {
                    case PieMode.Slice:
                        CoerceValue(EndAngleProperty);
                        break;

                    default:
                        CoerceValue(SliceProperty);
                        break;
                }
            }
            finally
            {
                IsUpdatingSweepDirection = false;
            }
        }

        private static object CoerceSweepDirectionValue(DependencyObject d, object value)
        {
            // keep SweepDirection in sync with EndAngle and StartAngle
            var pie = (Pie) d;
            if (pie.IsUpdatingEndAngle || pie.IsUpdatingStartAngle || pie.IsUpdatingMode)
            {
                if (DoubleHelper.AreVirtuallyEqual(pie.StartAngle, pie.EndAngle))
                    // if the values are equal, use previously coerced value
                    value = pie.SweepDirection;
                else
                    value = pie.EndAngle < pie.StartAngle ? SweepDirection.Counterclockwise : SweepDirection.Clockwise;
            }

            return value;
        }

        #endregion

        #region Private Fields

        private Rect _rect = Rect.Empty;
        private BitVector32 _cacheBits = new(0);

        #endregion
    }
}