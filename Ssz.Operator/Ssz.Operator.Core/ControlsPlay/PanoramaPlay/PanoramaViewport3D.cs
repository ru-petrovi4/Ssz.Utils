using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Panorama;
using Ssz.Operator.Core.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public class PanoramaViewport3D : Viewport3D, IDisposable
    {
        #region construction and destruction

        public PanoramaViewport3D()
        {
            _panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            _camera = new PerspectiveCamera();

            PanoramaVisual3D = new Viewport2DVisual3D();
        }


        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        private void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                // Release and Dispose managed resources.
                PanoramaVisual3D.Visual = null;
                if (_currentPlayDsPageDrawingCanvas is not null)
                {
                    _currentPlayDsPageDrawingCanvas.Dispose();
                    _currentPlayDsPageDrawingCanvas = null;
                }
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            _disposed = true;
        }


        ~PanoramaViewport3D()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool IsActive { get; set; }
        public QuaternionRotation3D PanoramaRotation3D { get; } = new();
        public Viewport2DVisual3D PanoramaVisual3D { get; }


        public double GetViewAzimuth()
        {
            return Utils.NormalizeAngleInDegrees(_rotationZ + _panoramaDsPageType
                ?.LeftEdgeAzimuth ?? 0.0);
        }

        public void Init(PanoramaPlayControl panoramaPlayControl)
        {
            _panoramaPlayControl = panoramaPlayControl;

            _fieldOfViewMin = 50;
            _fieldOfViewMax = 110;

            // Camera Initialize            
            _camera.Position = new Point3D(0, 0, 0);
            _camera.UpDirection = new Vector3D(0, 0, 1);
            _camera.LookDirection = new Vector3D(1, 0, 0);
            Camera = _camera;

            // Light Model Initialize
            var lightModel = new ModelVisual3D();
            lightModel.Content = new DirectionalLight(Colors.White, new Vector3D(1, 0, 0));
            Children.Add(lightModel);

            // PanoramaVisual3D Initialize            
            var cacheMode = 1.0;
            var cacheModeString = DsProject.Instance.AddonsCommandLineOptions.TryGetValue("Pano.CacheMode");
            if (cacheModeString is not null)
            {
                double tmp;
                if (double.TryParse(cacheModeString, NumberStyles.Any, CultureInfo.InvariantCulture, out tmp))
                    cacheMode = tmp;
            }

            PanoramaVisual3D.CacheMode = new BitmapCache(cacheMode);

            var rotateTransform = new RotateTransform3D();
            rotateTransform.Rotation = PanoramaRotation3D;
            PanoramaVisual3D.Transform = rotateTransform;
            PanoramaVisual3D.Material = new DiffuseMaterial();
            Viewport2DVisual3D.SetIsVisualHostMaterial(PanoramaVisual3D.Material, true);
            Children.Add(PanoramaVisual3D);

            SizeChanged += OnSizeChanged;

            _panoramaAddon.PanoPointsCollection.CurrentPathChanged +=
                () => ShowPathOnCurrentPlayDsPageDrawingCanvas(false);
        }


        public void ValidateAndSetFieldOfView(double value)
        {
            if (value > _fieldOfViewMax) value = _fieldOfViewMax;
            else if (value < _fieldOfViewMin) value = _fieldOfViewMin;
            var horizontalFieldOfViewTanMax = Math.Tan((_upAngle - _downAngle) * Math.PI / 360) * ActualWidth /
                                              ActualHeight;
            var horizontalFieldOfViewAngleMax = Math.Atan(horizontalFieldOfViewTanMax) * 360 / Math.PI;
            if (value > horizontalFieldOfViewAngleMax) value = horizontalFieldOfViewAngleMax;
            _fieldOfView = value;
        }

        public void ValidateAndSetRotationY(double value)
        {
            var verticalFieldOfViewTan = Math.Tan(_fieldOfView * Math.PI / 360) * ActualHeight / ActualWidth;
            var verticalFieldOfViewAngle = Math.Atan(verticalFieldOfViewTan) * 360 / (2 * Math.PI);
            var rotationYMax = _upAngle - verticalFieldOfViewAngle;
            var rotationYMin = _downAngle + verticalFieldOfViewAngle;
            if (rotationYMax < rotationYMin)
            {
                var middleAngle = (rotationYMax + rotationYMin) / 2;
                rotationYMax = middleAngle;
                rotationYMin = middleAngle;
            }

            if (value > rotationYMax) value = rotationYMax;
            if (value < rotationYMin) value = rotationYMin;
            _rotationY = value;
        }

        public void ValidateAndSetRotationZ(double value)
        {
            _rotationZ = Utils.NormalizeAngleInDegrees(value);

            if (_panoramaPlayControl is null) return;
            _panoramaPlayControl.CompassControl.SetViewAzimuth(GetViewAzimuth());
        }


        public async void JumpAsync(PanoramaJumpDsCommandOptions? panoramaJumpDsCommandOptions,
            DsPageDrawing dsPageDrawing)
        {
            _panoramaDsPageType = dsPageDrawing.DsPageTypeObject as PanoramaDsPageType;
            if (_panoramaDsPageType is null) return;
            if (_panoramaPlayControl is null) return;

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            _panoPoint = panoramaAddon.PanoPointsCollection.PanoPointsDictionary.TryGetValue(dsPageDrawing.Name);
            if (_panoPoint is not null)
                if (double.IsNaN(_panoPoint.X) ||
                    double.IsNaN(_panoPoint.Y) ||
                    double.IsNaN(_panoPoint.Z))
                    _panoPoint = null;
            if (_panoPoint is not null)
            {
                panoramaAddon.PanoPointsCollection.SetCurrentPoint(_panoPoint);
            }
            else
            {
                if (_panoramaPlayControl.LinesViewport3D.IsInited)
                    _panoramaPlayControl.LinesViewport3D.PanoPoint = null;
            }

            //Opacity = 0;

            _currentPlayDsPageDrawingCanvas =
                new PlayDsPageDrawingCanvas(dsPageDrawing,
                    _panoramaPlayControl.PlayWindow.MainFrame);

            ShowPathOnCurrentPlayDsPageDrawingCanvas(true);

            PanoramaViewport3D previousPanoramaViewport3D = _panoramaPlayControl.PreviousPanoramaViewport3D;
            var animationDurationMs = panoramaAddon.AnimationDurationMs;
            var smoothJump = panoramaJumpDsCommandOptions is not null &&
                             previousPanoramaViewport3D._panoramaDsPageType is not null &&
                             Math.Abs(ObsoleteAnyHelper.ConvertTo<double>(panoramaJumpDsCommandOptions.VerticalDelta, false)) <
                             0.5 &&
                             animationDurationMs > 0 && animationDurationMs < 30000;

            double finalViewAzimuth;
            if (previousPanoramaViewport3D._panoramaDsPageType is not null)
            {
                if (smoothJump)
                {
                    if (panoramaJumpDsCommandOptions is null) throw new InvalidOperationException();
                    finalViewAzimuth =
                        Utils.NormalizeAngleInDegrees(
                            previousPanoramaViewport3D._panoramaDsPageType.LeftEdgeAzimuth +
                            panoramaJumpDsCommandOptions.JumpHorizontalK * 360);
                }
                else
                {
                    finalViewAzimuth = previousPanoramaViewport3D.GetViewAzimuth();
                }
            }
            else
            {
                finalViewAzimuth = _panoramaDsPageType.DefaultViewAzimuth;
            }

            Utils.CalculateVerticalAngles(_panoramaDsPageType, dsPageDrawing.Width, dsPageDrawing.Height,
                out _verticalImageAngle, out _upAngle, out _downAngle);

            var geometry3D = await GetPanoramaVisual3DGeometryAsync();
            if (!ReferenceEquals(PanoramaVisual3D.Geometry, geometry3D))
                PanoramaVisual3D.Geometry = geometry3D;

            ValidateAndSetFieldOfView(DefaultFieldOfView);
            ValidateAndSetRotationZ(finalViewAzimuth - _panoramaDsPageType.LeftEdgeAzimuth);
            ValidateAndSetRotationY(0.0);

            PanoramaRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                                            new Quaternion(AxisZ, _rotationZ);

            _camera.FieldOfView = _fieldOfView;

            _currentPlayDsPageDrawingCanvas.ClipToBounds = true;
            var padding = GetDrawingCanvasPadding(dsPageDrawing.Width, dsPageDrawing.Height);
            PanoramaVisual3D.Visual = new Border
            {
                Padding = padding,
                Background = dsPageDrawing.ComputeDsPageBackgroundBrush(),
                Child = _currentPlayDsPageDrawingCanvas
            };

            InvalidateVisual();

            if (smoothJump)
            {
                await Dispatcher.BeginInvoke(new Action(async () =>
                {
                    await Task.Delay(50);

                    AnimatedTransition(animationDurationMs);
                }), DispatcherPriority.Background);
            }
            else
            {
                await Task.Delay(50);

                OnTransitionFinished();
            }
        }

        public async Task<Geometry3D?> GetPanoramaVisual3DGeometryAsync()
        {
            return await Task.Run(() =>
            {
                switch (_panoramaDsPageType?.PanoramaType ?? PanoramaType.Spherical)
                {
                    case PanoramaType.Spherical:
                    {
                        if (_sphericalGeometry3D is null)
                            _sphericalGeometry3D = NewPanoramaGeometry(PanoramaType.Spherical);
                        return _sphericalGeometry3D;
                    }
                    case PanoramaType.Cylindrical:
                    {
                        if (_cylindricalGeometry3D is null)
                            _cylindricalGeometry3D = NewPanoramaGeometry(PanoramaType.Cylindrical);
                        return _cylindricalGeometry3D;
                    }
                }

                return null;
            });
        }


        public Thickness GetDrawingCanvasPadding(double drawingWidth, double drawingHeight)
        {
            double topMargin = 0;
            double bottomMargin = 0;
            double leftMargin = 0;
            double rightMargin = 0;

            if (_panoramaDsPageType is null) return new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);

            switch (_panoramaDsPageType.PanoramaType)
            {
                case PanoramaType.Cylindrical:
                {
                    if (_upAngle >= CylinderUpDownAngle)
                    {
                        _downAngle = _downAngle - _upAngle + CylinderUpDownAngle;
                        _upAngle = CylinderUpDownAngle;
                    }

                    if (_downAngle <= -CylinderUpDownAngle)
                    {
                        _upAngle = _upAngle - _downAngle - CylinderUpDownAngle;
                        _downAngle = -CylinderUpDownAngle;
                    }

                    var upImageSize = Math.Tan(_upAngle * Math.PI / 180.0);
                    var downImageSize = Math.Tan(-_downAngle * Math.PI / 180.0);
                    var verticalImageSize = upImageSize + downImageSize;
                    var cylinderUpDownSize = Math.Tan(CylinderUpDownAngle * Math.PI / 180.0);
                    topMargin = drawingHeight * (cylinderUpDownSize - upImageSize) /
                                verticalImageSize;
                    bottomMargin = drawingHeight *
                        (cylinderUpDownSize - downImageSize) / verticalImageSize;

                    var width360 = drawingWidth * 360.0 / _panoramaDsPageType.HorizontalImageAngle;
                    leftMargin = rightMargin = (width360 - drawingWidth) / 2.0;
                }
                    break;
                case PanoramaType.Spherical:
                {
                    topMargin = drawingHeight * (90.0 - _upAngle) / _verticalImageAngle;
                    bottomMargin = drawingHeight * (90.0 + _downAngle) /
                                   _verticalImageAngle;

                    var width360 = drawingWidth * 360.0 / _panoramaDsPageType.HorizontalImageAngle;
                    leftMargin = rightMargin = (width360 - drawingWidth) / 2.0;
                }
                    break;
            }

            if (topMargin < 0) topMargin = 0;
            if (bottomMargin < 0) bottomMargin = 0;

            return new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
        }

        public void OnMouseDown(Point position)
        {
            _downPoint = position;
            _rotationVector.X = _rotationY;
            _rotationVector.Y = _rotationZ;
        }

        public void OnMouseUp()
        {
            _downPoint = null;
        }

        public void OnMouseMove(Point position)
        {
            if (!_downPoint.HasValue) return;
            var offset = Point.Subtract(position, _downPoint.Value) * 0.25;

            ValidateAndSetRotationZ(_rotationVector.Y - offset.X);
            ValidateAndSetRotationY(_rotationVector.X + offset.Y);

            PanoramaRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                                            new Quaternion(AxisZ, _rotationZ);

            if (_panoramaPlayControl is not null && _panoramaPlayControl.LinesViewport3D.IsInited)
                _panoramaPlayControl.LinesViewport3D.MapRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                    new Quaternion(AxisZ, GetViewAzimuth());
        }

        public void OnMouseWheel(int delta)
        {
            ValidateAndSetFieldOfView(_fieldOfView - delta / 120 * 5);
            ValidateAndSetRotationY(_rotationY);

            PanoramaRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                                            new Quaternion(AxisZ, _rotationZ);

            _camera.FieldOfView = _fieldOfView;

            if (_panoramaPlayControl is not null && _panoramaPlayControl.LinesViewport3D.IsInited)
            {
                _panoramaPlayControl.LinesViewport3D.PerspectiveCamera.FieldOfView = _camera.FieldOfView;
                _panoramaPlayControl.LinesViewport3D.MapRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                    new Quaternion(AxisZ, GetViewAzimuth());
            }
        }

        #endregion

        #region protected functions

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseDown(e);

            if (!IsActive) return;

            OnMouseDown(e.GetPosition(this));
        }

        protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseUp(e);

            if (!IsActive) return;

            OnMouseUp();
        }

        protected override void OnPreviewMouseMove(MouseEventArgs e)
        {
            base.OnPreviewMouseMove(e);

            if (!IsActive) return;

            if (e.LeftButton == MouseButtonState.Released) return;

            OnMouseMove(e.GetPosition(this));
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            if (!IsActive) return;

            OnMouseWheel(e.Delta);
        }

        #endregion

        #region private functions

        private static Point3D GetSphericalPosition(double t, double z)
        {
            var r = Math.Sqrt(1 - z * z);
            var x = r * Math.Cos(t);
            var y = r * Math.Sin(t);

            return new Point3D(x, y, z);
        }

        private static Vector3D GetSphericalNormal(double t, double z)
        {
            return -(Vector3D) GetSphericalPosition(t, z);
        }

        private static Point GetSphericalTextureCoordinate(double t, double z)
        {
            return new(1 - t / (2.0 * Math.PI), (Math.PI / 2 - Math.Asin(z)) / Math.PI);
        }

        private static Point3D GetCylindricalPosition(double t, double z)
        {
            double r = 1;
            var x = r * Math.Cos(t);
            var y = r * Math.Sin(t);

            return new Point3D(x, y, z);
        }

        private static Vector3D GetCylindricalNormal(double t, double z)
        {
            var v = GetCylindricalPosition(t, z);
            v.Z = 0;
            return -(Vector3D) v;
        }

        private static Point GetCylindricalTextureCoordinate(double t, double z, double zMin, double zMax)
        {
            return new(1 - t / (2.0 * Math.PI), (zMax - z) / (zMax - zMin));
        }

        private void ShowPathOnCurrentPlayDsPageDrawingCanvas(bool firstTime)
        {
            if (_currentPlayDsPageDrawingCanvas is null || _panoramaPlayControl is null) return;

            var complexDsShapeViews =
                TreeHelper.FindChilds<ComplexDsShapeView>(_currentPlayDsPageDrawingCanvas).ToArray();
            var initOnMouseOver = firstTime && _panoramaPlayControl.LinesViewport3D.IsInited;

            var panoramaAddon = DsProject.Instance.GetAddon<PanoramaAddon>();
            if (initOnMouseOver || panoramaAddon.PanoPointsCollection.CurrentPath.Length > 0)
            {
                ComplexDsShapeView? complexDsShapeViewWithMaxIndexInPath = null;
                var maxIndex =
                    panoramaAddon.PanoPointsCollection.CurrentPathIndex(_currentPlayDsPageDrawingCanvas
                        .PlayDrawingViewModel.Drawing.Name);
                foreach (ComplexDsShapeView complexDsShapeView in complexDsShapeViews)
                {
                    complexDsShapeView.IsHighlighted = false;
                    foreach (DsShapeViewBase dsShapeView in complexDsShapeView.DsShapeViews)
                    {
                        var buttonDsShapeView = dsShapeView as ButtonDsShapeViewBase;
                        if (buttonDsShapeView is null) continue;
                        var buttonDsShape = (ButtonDsShapeBase) buttonDsShapeView.DsShapeViewModel.DsShape;
                        if (buttonDsShape.ClickDsCommand.Command != CommandsManager.PanoramaJumpCommand) continue;

                        if (initOnMouseOver)
                        {
                            complexDsShapeView.MouseEnter += ComplexDsShapeViewOnMouseEnter;
                            complexDsShapeView.MouseLeave += ComplexDsShapeViewOnMouseLeave;
                        }

                        var p = (PanoramaJumpDsCommandOptions) (buttonDsShape.ClickDsCommand.DsCommandOptions ??
                                                                throw new InvalidOperationException()).Clone();
                        p.ReplaceConstants(buttonDsShape.Container);

                        var index =
                            panoramaAddon.PanoPointsCollection.CurrentPathIndex(
                                Path.GetFileNameWithoutExtension(p.FileRelativePath ?? ""));
                        if (index > maxIndex)
                        {
                            maxIndex = index;
                            complexDsShapeViewWithMaxIndexInPath = complexDsShapeView;
                        }
                    }
                }

                if (complexDsShapeViewWithMaxIndexInPath is not null)
                    complexDsShapeViewWithMaxIndexInPath.IsHighlighted = true;
            }
            else
            {
                foreach (ComplexDsShapeView complexDsShapeView in complexDsShapeViews)
                    complexDsShapeView.IsHighlighted = false;
            }
        }

        private void ComplexDsShapeViewOnMouseEnter(object? sender, MouseEventArgs e)
        {
            if (_panoramaPlayControl is null) throw new InvalidOperationException();
            _panoramaPlayControl.LinesViewport3D.Visibility = Visibility.Visible;
        }

        private void ComplexDsShapeViewOnMouseLeave(object? sender, MouseEventArgs e)
        {
            if (_panoramaPlayControl is null) throw new InvalidOperationException();
            _panoramaPlayControl.LinesViewport3D.Visibility = Visibility.Hidden;
        }

        private void OnTransitionFinished()
        {
            if (_panoramaPlayControl is null) throw new InvalidOperationException();
            PanoramaViewport3D previousPanoramaViewport3D = _panoramaPlayControl.PreviousPanoramaViewport3D;

            previousPanoramaViewport3D.PanoramaVisual3D.Visual = null;
            if (previousPanoramaViewport3D._currentPlayDsPageDrawingCanvas is not null)
            {
                previousPanoramaViewport3D._currentPlayDsPageDrawingCanvas.Dispose();
                previousPanoramaViewport3D._currentPlayDsPageDrawingCanvas = null;
            }

            previousPanoramaViewport3D.IsActive = false;
            IsActive = true;
            Panel.SetZIndex(previousPanoramaViewport3D, 0);
            Panel.SetZIndex(this, 1);

            _camera.FieldOfView = _fieldOfView;
            PanoramaRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                                            new Quaternion(AxisZ, _rotationZ);

            if (_panoramaPlayControl.LinesViewport3D.IsInited)
            {
                _panoramaPlayControl.LinesViewport3D.MapTranslateTransform3D.OffsetX = -_panoPoint?.X ?? 0.0;
                _panoramaPlayControl.LinesViewport3D.MapTranslateTransform3D.OffsetY = -_panoPoint?.Y ?? 0.0;

                _panoramaPlayControl.LinesViewport3D.PerspectiveCamera.FieldOfView = _camera.FieldOfView;
                _panoramaPlayControl.LinesViewport3D.MapRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                    new Quaternion(AxisZ, GetViewAzimuth());

                _panoramaPlayControl.LinesViewport3D.PanoPoint = _panoPoint;
            }

            //Opacity = 1;
        }

        private void AnimatedTransition(int animationDurationMs)
        {
            if (_panoramaPlayControl is null) return;
            PanoramaViewport3D previousPanoramaViewport3D = _panoramaPlayControl.PreviousPanoramaViewport3D;

            const double firstPartAnimationPartsRatio = 0.7 / 1.0;
            const double secondPartAnimationPartsRatio = 0.3 / 1.0;
            var animationDuration = new Duration(TimeSpan.FromMilliseconds(animationDurationMs));
            var firstPartAnimationDuration =
                new Duration(TimeSpan.FromMilliseconds(firstPartAnimationPartsRatio * animationDurationMs));
            var secondPartAnimationBeginTime =
                TimeSpan.FromMilliseconds((1 - secondPartAnimationPartsRatio) * animationDurationMs);
            var secondPartAnimationDuration =
                new Duration(TimeSpan.FromMilliseconds(secondPartAnimationPartsRatio * animationDurationMs));

            var myStoryboard = new Storyboard();

            // Opacity Animation previousPanoramaViewport3D
            var opacityAnimation = new DoubleAnimation();
            opacityAnimation.From = 1;
            opacityAnimation.To = 0;
            opacityAnimation.Duration = firstPartAnimationDuration;
            Storyboard.SetTargetName(opacityAnimation, previousPanoramaViewport3D.Name);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            myStoryboard.Children.Add(opacityAnimation);
            /*
            // Opacity Animation thisPanoramaViewport3D
            opacityAnimation = new DoubleAnimation();
            opacityAnimation.From = 0;
            opacityAnimation.To = 1;
            opacityAnimation.BeginTime = secondPartAnimationBeginTime;
            opacityAnimation.Duration = secondPartAnimationDuration;
            Storyboard.SetTargetName(opacityAnimation, Name);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("Opacity"));
            myStoryboard.Children.Add(opacityAnimation);
            */

            // FieldOfView Animation previousPanoramaViewport3D
            var fieldOfViewAnimation = new DoubleAnimation();
            var fieldOfViewFrom = previousPanoramaViewport3D._fieldOfView;
            double fieldOfViewTo = 70;
            if (fieldOfViewTo < 1 || fieldOfViewTo > _fieldOfViewMax) fieldOfViewTo = _fieldOfViewMin;
            var fieldOfViewDelta = fieldOfViewTo - fieldOfViewFrom;
            fieldOfViewAnimation.From = fieldOfViewFrom;
            fieldOfViewAnimation.To = fieldOfViewTo;
            fieldOfViewAnimation.Duration = firstPartAnimationDuration;
            Storyboard.SetTargetName(fieldOfViewAnimation, previousPanoramaViewport3D.Name);
            Storyboard.SetTargetProperty(fieldOfViewAnimation, new PropertyPath("Camera.FieldOfView"));
            myStoryboard.Children.Add(fieldOfViewAnimation);
            /*
            // FieldOfView Animation thisPanoramaViewport3D
            fieldOfViewAnimation = new DoubleAnimation();
            fieldOfViewTo = _fieldOfView;
            fieldOfViewFrom = fieldOfViewTo - fieldOfViewDelta * 0.1;
            fieldOfViewAnimation.From = fieldOfViewFrom;
            fieldOfViewAnimation.To = fieldOfViewTo;
            fieldOfViewAnimation.BeginTime = secondPartAnimationBeginTime;
            fieldOfViewAnimation.Duration = secondPartAnimationDuration;
            Storyboard.SetTargetName(fieldOfViewAnimation, Name);
            Storyboard.SetTargetProperty(fieldOfViewAnimation, new PropertyPath("Camera.FieldOfView"));
            myStoryboard.Children.Add(fieldOfViewAnimation);*/

            var azimuthFrom = previousPanoramaViewport3D.GetViewAzimuth();
            var azimuthTo = GetViewAzimuth();
            var rotationZDelta = Utils.NormalizeAngle2InDegrees(azimuthTo - azimuthFrom);

            var rotationYFrom = previousPanoramaViewport3D._rotationY;
            var rotationYTo = _rotationY;

            // Rotation Animation previousPanoramaViewport3D
            var quaternionAnimation = new QuaternionAnimation();
            quaternionAnimation.From = new Quaternion(AxisY, rotationYFrom) *
                                       new Quaternion(AxisZ, previousPanoramaViewport3D._rotationZ);
            quaternionAnimation.To = new Quaternion(AxisY, rotationYTo) *
                                     new Quaternion(AxisZ, previousPanoramaViewport3D._rotationZ + rotationZDelta);
            quaternionAnimation.Duration = animationDuration;
            quaternionAnimation.DecelerationRatio = 0.1;
            Storyboard.SetTargetName(quaternionAnimation, previousPanoramaViewport3D.Name + @"PanoramaRotation3D");
            Storyboard.SetTargetProperty(quaternionAnimation, new PropertyPath("Quaternion"));
            myStoryboard.Children.Add(quaternionAnimation);

            // Rotation Animation thisPanoramaViewport3D
            quaternionAnimation = new QuaternionAnimation();
            quaternionAnimation.From = new Quaternion(AxisY, rotationYFrom) *
                                       new Quaternion(AxisZ, _rotationZ - rotationZDelta);
            quaternionAnimation.To = new Quaternion(AxisY, rotationYTo) *
                                     new Quaternion(AxisZ, _rotationZ);
            quaternionAnimation.Duration = animationDuration;
            quaternionAnimation.DecelerationRatio = 0.1;
            Storyboard.SetTargetName(quaternionAnimation, Name + @"PanoramaRotation3D");
            Storyboard.SetTargetProperty(quaternionAnimation, new PropertyPath("Quaternion"));
            myStoryboard.Children.Add(quaternionAnimation);

            if (_panoramaPlayControl.LinesViewport3D.IsInited && _panoPoint is not null &&
                previousPanoramaViewport3D._panoPoint is not null)
            {
                // FieldOfView Animation thisPanoramaViewport3D
                fieldOfViewAnimation = new DoubleAnimation();
                fieldOfViewFrom = previousPanoramaViewport3D._fieldOfView;
                fieldOfViewTo = _fieldOfView;
                fieldOfViewAnimation.From = fieldOfViewFrom;
                fieldOfViewAnimation.To = fieldOfViewTo;
                fieldOfViewAnimation.Duration = animationDuration;
                Storyboard.SetTargetName(fieldOfViewAnimation, _panoramaPlayControl.LinesViewport3D.Name);
                Storyboard.SetTargetProperty(fieldOfViewAnimation, new PropertyPath("Camera.FieldOfView"));
                myStoryboard.Children.Add(fieldOfViewAnimation);

                // Map Rotation Animation
                quaternionAnimation = new QuaternionAnimation();
                quaternionAnimation.From = new Quaternion(AxisY, rotationYFrom) *
                                           new Quaternion(AxisZ, azimuthFrom);
                quaternionAnimation.To = new Quaternion(AxisY, rotationYTo) *
                                         new Quaternion(AxisZ, azimuthTo);
                quaternionAnimation.Duration = animationDuration;
                quaternionAnimation.DecelerationRatio = 0.1;
                Storyboard.SetTargetName(quaternionAnimation, @"MapRotation3D");
                Storyboard.SetTargetProperty(quaternionAnimation, new PropertyPath("Quaternion"));
                myStoryboard.Children.Add(quaternionAnimation);

                // Map Animation X
                var animationX = new DoubleAnimation();
                animationX.From = -previousPanoramaViewport3D._panoPoint.X;
                animationX.To = -_panoPoint.X;
                animationX.Duration = animationDuration;
                //animationX.DecelerationRatio = 0.1;
                Storyboard.SetTargetName(animationX, @"MapTranslateTransform3D");
                Storyboard.SetTargetProperty(animationX, new PropertyPath("OffsetX"));
                myStoryboard.Children.Add(animationX);

                // Map Animation Y
                var animationY = new DoubleAnimation();
                animationY.From = -previousPanoramaViewport3D._panoPoint.Y;
                animationY.To = -_panoPoint.Y;
                animationY.Duration = animationDuration;
                //animationY.DecelerationRatio = 0.1;
                Storyboard.SetTargetName(animationY, @"MapTranslateTransform3D");
                Storyboard.SetTargetProperty(animationY, new PropertyPath("OffsetY"));
                myStoryboard.Children.Add(animationY);
            }

            myStoryboard.Completed += (sender, args) =>
            {
                myStoryboard.Stop((FrameworkElement) Parent);

                OnTransitionFinished();
            };

            myStoryboard.Begin((FrameworkElement) Parent, true);
        }

        private void OnSizeChanged(object? sender, SizeChangedEventArgs sizeChangedEventArgs)
        {
            if (!IsActive || _panoramaPlayControl is null) return;

            ValidateAndSetFieldOfView(_fieldOfView);
            ValidateAndSetRotationY(_rotationY);

            PanoramaRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                                            new Quaternion(AxisZ, _rotationZ);

            _camera.FieldOfView = _fieldOfView;

            if (_panoramaPlayControl.LinesViewport3D.IsInited)
            {
                _panoramaPlayControl.LinesViewport3D.PerspectiveCamera.FieldOfView = _camera.FieldOfView;
                _panoramaPlayControl.LinesViewport3D.MapRotation3D.Quaternion = new Quaternion(AxisY, _rotationY) *
                    new Quaternion(AxisZ, GetViewAzimuth());
            }
        }

        private Geometry3D NewPanoramaGeometry(PanoramaType panoramaType)
        {
            double zMin = 0;
            double zMax = 0;

            switch (panoramaType)
            {
                case PanoramaType.Spherical:
                    zMax = 1;
                    zMin = -1;
                    break;
                case PanoramaType.Cylindrical:
                    zMax = Math.Tan(CylinderUpDownAngle * 2.0 * Math.PI / 360.0);
                    zMin = Math.Tan(-CylinderUpDownAngle * 2.0 * Math.PI / 360.0);
                    break;
            }

            var dt = 2.0 * Math.PI / TiMax;
            var dz = (zMax - zMin) / ZiMax;

            var mesh = new MeshGeometry3D();

            switch (panoramaType)
            {
                case PanoramaType.Spherical:
                    for (var zi = 0; zi <= ZiMax; zi += 1)
                    {
                        var z = zMin + zi * dz;

                        for (var ti = 0; ti <= TiMax; ti += 1)
                        {
                            var t = ti * dt; // radians

                            mesh.Positions.Add(GetSphericalPosition(t, z));
                            mesh.Normals.Add(GetSphericalNormal(t, z));
                            mesh.TextureCoordinates.Add(GetSphericalTextureCoordinate(t, z));
                        }
                    }

                    break;
                case PanoramaType.Cylindrical:
                    for (var zi = 0; zi <= ZiMax; zi += 1)
                    {
                        var z = zMin + zi * dz;

                        for (var ti = 0; ti <= TiMax; ti += 1)
                        {
                            var t = ti * dt; // radians

                            mesh.Positions.Add(GetCylindricalPosition(t, z));
                            mesh.Normals.Add(GetCylindricalNormal(t, z));
                            mesh.TextureCoordinates.Add(GetCylindricalTextureCoordinate(t, z, zMin, zMax));
                        }
                    }

                    break;
            }

            for (var zi = 0; zi < ZiMax; zi += 1)
            for (var ti = 0; ti < TiMax; ti += 1)
            {
                var beginIndexZ0 = zi * (TiMax + 1);
                var beginIndexZ1 = (zi + 1) * (TiMax + 1);

                mesh.TriangleIndices.Add(beginIndexZ0 + ti + 1);
                mesh.TriangleIndices.Add(beginIndexZ0 + ti);
                mesh.TriangleIndices.Add(beginIndexZ1 + ti);

                mesh.TriangleIndices.Add(beginIndexZ1 + ti);
                mesh.TriangleIndices.Add(beginIndexZ1 + ti + 1);
                mesh.TriangleIndices.Add(beginIndexZ0 + ti + 1);
            }

            mesh.Freeze();
            return mesh;
        }

        #endregion

        #region private fields

        private static readonly Vector3D AxisY = new(0, 1, 0);
        private static readonly Vector3D AxisZ = new(0, 0, 1);
        private static Geometry3D? _sphericalGeometry3D;
        private static Geometry3D? _cylindricalGeometry3D;
        private PanoramaPlayControl? _panoramaPlayControl;
        private readonly PanoramaAddon _panoramaAddon;

        private bool _disposed;

        private readonly PerspectiveCamera _camera;

        private PanoramaDsPageType? _panoramaDsPageType;
        private PanoPoint? _panoPoint;


        private double _fieldOfView;


        private double _rotationY;


        private double _rotationZ;

        private double _upAngle;
        private double _downAngle;


        private double _fieldOfViewMin;


        private double _fieldOfViewMax;

        private Vector _rotationVector;
        private Point? _downPoint;
        private double _verticalImageAngle;

        private PlayDsPageDrawingCanvas? _currentPlayDsPageDrawingCanvas;

        private const double CylinderUpDownAngle = 60;
        private const int TiMax = 64;
        private const int ZiMax = 64;

        private const double DefaultFieldOfView = 100;

        #endregion
    }
}