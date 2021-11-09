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
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    public abstract class ShapeBase : Shape
    {
        #region Private Fields

        private Pen _pen;

        #endregion

        #region Constructors

        static ShapeBase()
        {
            StrokeDashArrayProperty.OverrideMetadata(typeof(ShapeBase), new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeDashCapProperty.OverrideMetadata(typeof(ShapeBase), new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeDashOffsetProperty.OverrideMetadata(typeof(ShapeBase),
                new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeEndLineCapProperty.OverrideMetadata(typeof(ShapeBase),
                new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeLineJoinProperty.OverrideMetadata(typeof(ShapeBase), new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeMiterLimitProperty.OverrideMetadata(typeof(ShapeBase),
                new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeProperty.OverrideMetadata(typeof(ShapeBase), new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeStartLineCapProperty.OverrideMetadata(typeof(ShapeBase),
                new FrameworkPropertyMetadata(OnStrokeChanged));
            StrokeThicknessProperty.OverrideMetadata(typeof(ShapeBase), new FrameworkPropertyMetadata(OnStrokeChanged));
        }

        #endregion

        #region IsPenEmptyOrUndefined Internal Property

        internal bool IsPenEmptyOrUndefined
        {
            get
            {
                var strokeThickness = StrokeThickness;
                return Stroke is null || DoubleHelper.IsNaN(strokeThickness) ||
                       DoubleHelper.AreVirtuallyEqual(0, strokeThickness);
            }
        }

        #endregion

        #region DefiningGeometry Protected Property

        protected abstract override Geometry DefiningGeometry { get; }

        #endregion

        internal virtual Rect GetDefiningGeometryBounds()
        {
            var geometry = DefiningGeometry;

            Debug.Assert(geometry is not null);

            return geometry.Bounds;
        }

        internal virtual Size GetNaturalSize()
        {
            var geometry = DefiningGeometry;

            Debug.Assert(geometry is not null);

            var bounds = geometry.GetRenderBounds(GetPen());

            return new Size(Math.Max(bounds.Right, 0), Math.Max(bounds.Bottom, 0));
        }

        internal Pen GetPen()
        {
            if (IsPenEmptyOrUndefined)
                return null;

            if (_pen is null) _pen = MakePen();

            return _pen;
        }

        internal double GetStrokeThickness()
        {
            if (IsPenEmptyOrUndefined)
                return 0d;

            return Math.Abs(StrokeThickness);
        }

        internal bool IsSizeEmptyOrUndefined(Size size)
        {
            return DoubleHelper.IsNaN(size.Width) || DoubleHelper.IsNaN(size.Height) || size.IsEmpty;
        }

        private static void OnStrokeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((ShapeBase) d)._pen = null;
        }

        private Pen MakePen()
        {
            var pen = new Pen();
            pen.Brush = Stroke;
            pen.DashCap = StrokeDashCap;
            if (StrokeDashArray is not null || StrokeDashOffset != 0.0)
                pen.DashStyle = new DashStyle(StrokeDashArray, StrokeDashOffset);
            pen.EndLineCap = StrokeEndLineCap;
            pen.LineJoin = StrokeLineJoin;
            pen.MiterLimit = StrokeMiterLimit;
            pen.StartLineCap = StrokeStartLineCap;
            pen.Thickness = Math.Abs(StrokeThickness);

            return pen;
        }
    }
}