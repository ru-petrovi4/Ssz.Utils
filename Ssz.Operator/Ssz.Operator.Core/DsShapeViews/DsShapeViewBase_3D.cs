/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2021
//                                  SIMCODE
//                              ALL RIGHTS RESERVED
//
//	Legal rights of Simcode in this software is distinct from
//  ownership of any medium in which the software is embodied. Copyright notices 
//  must be reproduced in any copies authorized by Simcode
//
///////////////////////////////////////////////////////////////////////////

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.DsShapeViews
{
    [ContentProperty("Content")]
    public abstract partial class DsShapeViewBase : FrameworkElement, IDisposable
    {
        private class LayoutInvalidationCatcher : Border
        {
            #region protected functions

            protected override Size MeasureOverride(Size constraint)
            {
                var pl = Parent as DsShapeViewBase;
                if (pl is not null) pl.InvalidateMeasure();
                return base.MeasureOverride(constraint);
            }

            protected override Size ArrangeOverride(Size arrangeSize)
            {
                var pl = Parent as DsShapeViewBase;
                if (pl is not null) pl.InvalidateArrange();
                return base.ArrangeOverride(arrangeSize);
            }

            #endregion
        }

        #region public functions

        public static readonly DependencyProperty ContentProperty =
            DependencyProperty.Register("Content", typeof(FrameworkElement), typeof(DsShapeViewBase),
                new UIPropertyMetadata(null, OnContentChanged));

        public static readonly DependencyProperty RotationXProperty =
            DependencyProperty.Register("RotationX", typeof(double), typeof(DsShapeViewBase),
                new UIPropertyMetadata(0.0, OnRotationChanged));

        public static readonly DependencyProperty RotationYProperty =
            DependencyProperty.Register("RotationY", typeof(double), typeof(DsShapeViewBase),
                new UIPropertyMetadata(0.0, OnRotationChanged));

        public static readonly DependencyProperty RotationZProperty =
            DependencyProperty.Register("RotationZ", typeof(double), typeof(DsShapeViewBase),
                new UIPropertyMetadata(0.0, OnRotationChanged));

        public static readonly DependencyProperty FieldOfViewProperty =
            DependencyProperty.Register("FieldOfView", typeof(double), typeof(DsShapeViewBase),
                new UIPropertyMetadata(45.0, (d, args) => ((DsShapeViewBase) d).Update3D()));

        public FrameworkElement? Content
        {
            get => _content;
            set
            {
                _content = value;
                SetValue(ContentProperty, value);
            }
        }

        public double RotationX
        {
            get => (double) GetValue(RotationXProperty);
            set => SetValue(RotationXProperty, value);
        }

        public double RotationY
        {
            get => (double) GetValue(RotationYProperty);
            set => SetValue(RotationYProperty, value);
        }

        public double RotationZ
        {
            get => (double) GetValue(RotationZProperty);
            set => SetValue(RotationZProperty, value);
        }

        public double FieldOfView
        {
            get => (double) GetValue(FieldOfViewProperty);
            set => SetValue(FieldOfViewProperty, value);
        }

        #endregion

        #region protected functions

        protected override Size MeasureOverride(Size availableSize)
        {
            if (!_is3DMode)
            {
                if (_visualChild is not null)
                    try
                    {
                        _visualChild.Measure(availableSize);
                        return _visualChild.DesiredSize;
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "DsShapeViewBase.MeasureOverride error.");
                        _visualChild = null;
                    }
            }
            else
            {
                if (_visualChild is not null && _logicalChild is not null)
                    try
                    {
                        Size result;
                        // Measure based on the size of the logical child, since we want to align with it.
                        _logicalChild.Measure(availableSize);
                        result = _logicalChild.DesiredSize;
                        _visualChild.Measure(result);
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "DsShapeViewBase.MeasureOverride error.");
                        _visualChild = null;
                        _logicalChild = null;
                    }
            }

            return new Size(0, 0);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (!_is3DMode)
            {
                if (_visualChild is not null)
                    try
                    {
                        _visualChild.Arrange(new Rect(finalSize));
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "DsShapeViewBase.ArrangeOverride error.");
                        _visualChild = null;
                    }
            }
            else
            {
                if (_visualChild is not null && _logicalChild is not null)
                    try
                    {
                        _logicalChild.Arrange(new Rect(finalSize));
                        _visualChild.Arrange(new Rect(finalSize));
                        Update3D();
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex, "DsShapeViewBase.ArrangeOverride error.");
                        _visualChild = null;
                        _logicalChild = null;
                    }
            }

            return base.ArrangeOverride(finalSize);
        }

        protected override Visual? GetVisualChild(int index)
        {
            return _visualChild;
        }

        protected override int VisualChildrenCount => _visualChild is null ? 0 : 1;

        #endregion

        #region private functions

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sv = (DsShapeViewBase) d;
            sv.OnContentChanged((FrameworkElement) e.NewValue);
        }

        private void OnContentChanged(FrameworkElement? newValue)
        {
            if (_logicalChild is not null) RemoveLogicalChild(_logicalChild);
            if (_visualChild is not null) RemoveVisualChild(_visualChild);

            var layoutInvalidationCatcher = _logicalChild as LayoutInvalidationCatcher;
            if (layoutInvalidationCatcher is not null) layoutInvalidationCatcher.Child = null;

            if (!_is3DMode)
            {
                _logicalChild = null;
                _visualChild = newValue;
            }
            else
            {
                _logicalChild = new LayoutInvalidationCatcher {Background = Brushes.Transparent, Child = newValue};
                _visualChild = CreateVisualChild3D();
            }

            if (_visualChild is not null) AddVisualChild(_visualChild);
            // Need to use a logical child here to make sure databinding toolkitOperations get down to it,
            // since otherwise the child appears only as the Visual to a Viewport2DVisual3D, which 
            // doesn't have databinding toolkitOperations pass into it from above.
            if (_logicalChild is not null) AddLogicalChild(_logicalChild);

            InvalidateMeasure();
        }

        private static void OnRotationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sv = (DsShapeViewBase) d;
            var is3DMode = sv.RotationX != 0.0 || sv.RotationY != 0.0 || sv.RotationZ != 0.0;
            if (is3DMode != sv._is3DMode)
            {
                sv._is3DMode = is3DMode;
                sv.OnContentChanged(sv.Content);
            }

            sv.UpdateRotation();
        }

        private FrameworkElement CreateVisualChild3D()
        {
            var simpleQuad = new MeshGeometry3D
            {
                Positions = new Point3DCollection(Mesh),
                TextureCoordinates = new PointCollection(TexCoords),
                TriangleIndices = new Int32Collection(Indices)
            };

            // Front material is interactive, back material is not.
            Material frontMaterial = new DiffuseMaterial(Brushes.White);
            frontMaterial.SetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty, true);

            var vb = new VisualBrush(_logicalChild);
            SetCachingForObject(vb); // big perf wins by caching!!
            Material backMaterial = new DiffuseMaterial(vb);

            _rotationTransform.Rotation = _quaternionRotation;
            var xfGroup = new Transform3DGroup {Children = {_scaleTransform, _rotationTransform}};

            var backModel = new GeometryModel3D
            {
                Geometry = simpleQuad,
                Transform = xfGroup,
                BackMaterial = backMaterial
            };
            var m3dGroup = new Model3DGroup
            {
                Children =
                {
                    new DirectionalLight(Colors.White, new Vector3D(0, 0, -1)),
                    new DirectionalLight(Colors.White, new Vector3D(0.1, -0.1, 1)),
                    backModel
                }
            };

            // Non-interactive Visual3D consisting of the backside, and two lights.
            var mv3d = new ModelVisual3D {Content = m3dGroup};

            // Interactive frontside Visual3D
            var frontModel = new Viewport2DVisual3D
            {
                Geometry = simpleQuad,
                Visual = _logicalChild,
                Material = frontMaterial,
                Transform = xfGroup
            };

            // Cache the brush in the VP2V3 by setting caching on it.  Big perf wins.
            SetCachingForObject(frontModel);

            // Scene consists of both the above Visual3D's.
            _viewport3D = new Viewport3D {ClipToBounds = false, Children = {mv3d, frontModel}};

            UpdateRotation();

            return _viewport3D;
        }

        private void SetCachingForObject(DependencyObject d)
        {
            RenderOptions.SetCachingHint(d, CachingHint.Cache);
            RenderOptions.SetCacheInvalidationThresholdMinimum(d, 0.5);
            RenderOptions.SetCacheInvalidationThresholdMaximum(d, 2.0);
        }

        private void UpdateRotation()
        {
            if (!_is3DMode) return;

            var qx = new Quaternion(XAxis, RotationX);
            var qy = new Quaternion(YAxis, RotationY);
            var qz = new Quaternion(ZAxis, RotationZ);

            _quaternionRotation.Quaternion = qx * qy * qz;
        }

        private void Update3D()
        {
            if (!_is3DMode) return;

            // Use GetDescendantBounds for sizing and centering since DesiredSize includes layout whitespace, whereas GetDescendantBounds 
            // is tighter
            var logicalBounds = VisualTreeHelper.GetDescendantBounds(_logicalChild);
            var w = logicalBounds.Width;
            var h = logicalBounds.Height;

            // Create a camera that looks down -Z, with up as Y, and positioned right halfway in X and Y on the element, 
            // and back along Z the right distance based on the field-of-view is the same dsProjected size as the 2D content
            // that it's looking at. 
            var fovInRadians = FieldOfView * (Math.PI / 180);
            var zValue = w / Math.Tan(fovInRadians / 2) / 2;
            if (_viewport3D is not null)
                _viewport3D.Camera = new PerspectiveCamera(new Point3D(w / 2, h / 2, zValue),
                    -ZAxis,
                    YAxis,
                    FieldOfView);

            _scaleTransform.ScaleX = w;
            _scaleTransform.ScaleY = h;
            _rotationTransform.CenterX = w / 2;
            _rotationTransform.CenterY = h / 2;
        }

        #endregion

        #region private fields

        private static readonly Point3D[] Mesh =
        {
            new(0, 0, 0), new(0, 1, 0), new(1, 1, 0),
            new(1, 0, 0)
        };

        private static readonly Point[] TexCoords =
        {
            new(0, 1), new(0, 0), new(1, 0), new(1, 1)
        };

        private static readonly int[] Indices = {0, 2, 1, 0, 3, 2};
        private static readonly Vector3D XAxis = new(1, 0, 0);
        private static readonly Vector3D YAxis = new(0, 1, 0);
        private static readonly Vector3D ZAxis = new(0, 0, 1);

        public bool _is3DMode;

        private FrameworkElement? _logicalChild;
        private FrameworkElement? _visualChild;

        private readonly QuaternionRotation3D _quaternionRotation = new();
        private readonly RotateTransform3D _rotationTransform = new();
        private Viewport3D? _viewport3D;
        private readonly ScaleTransform3D _scaleTransform = new();

        #endregion
    }
}