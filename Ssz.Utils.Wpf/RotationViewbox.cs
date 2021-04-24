/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2016
//                          SSZ INTERNATIONAL INC.
//                              ALL RIGHTS RESERVED
//
//	Legal rights of Ssz International Inc. in this software is distinct from
//  ownership of any medium in which the software is embodied. Copyright notices 
//  must be reproduced in any copies authorized by Ssz International Inc.
//
///////////////////////////////////////////////////////////////////////////
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Ssz.Utils.Wpf
{
    [ContentProperty("Content")]
    public class RotationViewbox : FrameworkElement
    {
        #region public functions

        public static readonly DependencyProperty RotationXProperty =
            DependencyProperty.Register("RotationX", typeof (double), typeof (RotationViewbox),
                new UIPropertyMetadata(0.0, (d, args) => ((RotationViewbox) d).UpdateRotation()));

        public static readonly DependencyProperty RotationYProperty =
            DependencyProperty.Register("RotationY", typeof (double), typeof (RotationViewbox),
                new UIPropertyMetadata(0.0, (d, args) => ((RotationViewbox) d).UpdateRotation()));

        public static readonly DependencyProperty RotationZProperty =
            DependencyProperty.Register("RotationZ", typeof (double), typeof (RotationViewbox),
                new UIPropertyMetadata(0.0, (d, args) => ((RotationViewbox) d).UpdateRotation()));

        public static readonly DependencyProperty FieldOfViewProperty =
            DependencyProperty.Register("FieldOfView", typeof (double), typeof (RotationViewbox),
                new UIPropertyMetadata(45.0, (d, args) => ((RotationViewbox) d).Update3D(),
                    (d, val) => Math.Min(Math.Max((double) val, 0.5), 179.9))); // clamp to a meaningful range


        public double RotationX
        {
            get { return (double) GetValue(RotationXProperty); }
            set { SetValue(RotationXProperty, value); }
        }

        public double RotationY
        {
            get { return (double) GetValue(RotationYProperty); }
            set { SetValue(RotationYProperty, value); }
        }

        public double RotationZ
        {
            get { return (double) GetValue(RotationZProperty); }
            set { SetValue(RotationZProperty, value); }
        }

        public double FieldOfView
        {
            get { return (double) GetValue(FieldOfViewProperty); }
            set { SetValue(FieldOfViewProperty, value); }
        }

        public FrameworkElement Content
        {
            get 
            {
                if (_originalContent == null) throw new InvalidOperationException();
                return _originalContent; 
            }
            set
            {
                if (_originalContent != value)
                {
                    RemoveVisualChild(_visualContent);
                    RemoveLogicalChild(_logicalContent);

                    // Wrap child with special decorator that catches layout invalidations. 
                    _originalContent = value;
                    _logicalContent = new LayoutInvalidationCatcher {Child = _originalContent};
                    _visualContent = CreateVisualChild();

                    AddVisualChild(_visualContent);

                    // Need to use a logical child here to make sure databinding operations get down to it,
                    // since otherwise the child appears only as the Visual to a Viewport2DVisual3D, which 
                    // doesn't have databinding operations pass into it from above.
                    AddLogicalChild(_logicalContent);
                    InvalidateMeasure();
                }
            }
        }

        #endregion

        #region protected functions

        protected override Size MeasureOverride(Size availableSize)
        {
            Size result;
            if (_logicalContent != null)
            {
                // Measure based on the size of the logical child, since we want to align with it.
                _logicalContent.Measure(availableSize);
                result = _logicalContent.DesiredSize;
                if (_visualContent == null) throw new InvalidOperationException();
                _visualContent.Measure(result);
            }
            else
            {
                result = new Size(0, 0);
            }
            return result;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (_logicalContent != null)
            {
                _logicalContent.Arrange(new Rect(finalSize));
                if (_visualContent == null) throw new InvalidOperationException();
                _visualContent.Arrange(new Rect(finalSize));
                Update3D();
            }
            return base.ArrangeOverride(finalSize);
        }

        protected override Visual GetVisualChild(int index)
        {
            if (_visualContent == null) throw new InvalidOperationException();
            return _visualContent;
        }

        protected override int VisualChildrenCount
        {
            get { return _visualContent == null ? 0 : 1; }
        }

        #endregion

        #region private functions

        private FrameworkElement CreateVisualChild()
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

            var vb = new VisualBrush(_logicalContent);
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
                Visual = _logicalContent,
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
            var qx = new Quaternion(XAxis, RotationX);
            var qy = new Quaternion(YAxis, RotationY);
            var qz = new Quaternion(ZAxis, RotationZ);

            _quaternionRotation.Quaternion = qx*qy*qz;
        }

        private void Update3D()
        {
            // Use GetDescendantBounds for sizing and centering since DesiredSize includes layout whitespace, whereas GetDescendantBounds 
            // is tighter
            Rect logicalBounds = VisualTreeHelper.GetDescendantBounds(_logicalContent);
            double w = logicalBounds.Width;
            double h = logicalBounds.Height;

            // Create a camera that looks down -Z, with up as Y, and positioned right halfway in X and Y on the element, 
            // and back along Z the right distance based on the field-of-view is the same projected size as the 2D content
            // that it's looking at. 
            double fovInRadians = FieldOfView*(Math.PI/180);
            double zValue = w/Math.Tan(fovInRadians/2)/2;
            if (_viewport3D == null) throw new InvalidOperationException();
            _viewport3D.Camera = new PerspectiveCamera(new Point3D(w/2, h/2, zValue),
                -ZAxis,
                YAxis,
                FieldOfView);


            _scaleTransform.ScaleX = w;
            _scaleTransform.ScaleY = h;
            _rotationTransform.CenterX = w/2;
            _rotationTransform.CenterY = h/2;
        }

        #endregion

        #region private fields

        private static readonly Point3D[] Mesh =
        {
            new Point3D(0, 0, 0), new Point3D(0, 1, 0), new Point3D(1, 1, 0),
            new Point3D(1, 0, 0)
        };

        private static readonly Point[] TexCoords =
        {
            new Point(0, 1), new Point(0, 0), new Point(1, 0), new Point(1, 1)
        };

        private static readonly int[] Indices = {0, 2, 1, 0, 3, 2};
        private static readonly Vector3D XAxis = new Vector3D(1, 0, 0);
        private static readonly Vector3D YAxis = new Vector3D(0, 1, 0);
        private static readonly Vector3D ZAxis = new Vector3D(0, 0, 1);

        private FrameworkElement? _logicalContent;
        private FrameworkElement? _visualContent;
        private FrameworkElement? _originalContent;

        private readonly QuaternionRotation3D _quaternionRotation = new QuaternionRotation3D();
        private readonly RotateTransform3D _rotationTransform = new RotateTransform3D();
        private Viewport3D? _viewport3D;
        private readonly ScaleTransform3D _scaleTransform = new ScaleTransform3D();

        #endregion

        /// <summary>
        ///     Wrap this around a class that we want to catch the measure and arrange
        ///     processes occuring on, and propagate to the parent RotationViewbox, if any.
        ///     Do this because layout invalidations don't flow up out of a
        ///     Viewport2DVisual3D object.
        /// </summary>
        private class LayoutInvalidationCatcher : Decorator
        {
            #region protected functions

            protected override Size MeasureOverride(Size constraint)
            {
                var pl = Parent as RotationViewbox;
                if (pl != null)
                {
                    pl.InvalidateMeasure();
                }
                return base.MeasureOverride(constraint);
            }

            protected override Size ArrangeOverride(Size arrangeSize)
            {
                var pl = Parent as RotationViewbox;
                if (pl != null)
                {
                    pl.InvalidateArrange();
                }
                return base.ArrangeOverride(arrangeSize);
            }

            #endregion
        }
    }
}