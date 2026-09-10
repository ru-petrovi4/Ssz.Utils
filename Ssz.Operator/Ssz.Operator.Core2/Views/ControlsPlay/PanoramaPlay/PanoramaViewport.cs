using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Skia;
using SkiaSharp;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.DsPageTypes;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     What PanoramaViewport3D was in WPF: the panorama page seen through a perspective camera sitting
    ///     in the middle of a textured sphere or cylinder.
    ///     WPF put the page on a Viewport2DVisual3D and let the 3D pipeline do the rest. Avalonia has no 3D,
    ///     so the mesh is projected here and handed to Skia as a triangle list; the camera model, the angle
    ///     limits and the drag / wheel arithmetic are the ones of the WPF control, so the view behaves the
    ///     same.
    /// </summary>
    public class PanoramaViewport : Control, IDisposable
    {
        #region construction and destruction

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;
            UnwatchPage();
            DisposeTextureBuffers();
            _texture.Dispose();
            _playDsPageDrawingCanvas?.Dispose();
            _playDsPageDrawingCanvas = null;
        }

        #endregion

        #region public functions

        public bool IsActive { get; set; }

        public PanoramaDsPageType? PanoramaDsPageType => _panoramaDsPageType;

        /// <summary>
        ///     Raised whenever the camera moved, so the layer with the shapes of the page can follow it.
        /// </summary>
        public event Action? CameraChanged;

        /// <summary>
        ///     The camera, as the transition between two points animates it. The values are taken as they
        ///     are: they come from the ones the limits were already applied to.
        /// </summary>
        public double FieldOfView => _fieldOfView;

        public double RotationY => _rotationY;

        public double RotationZ => _rotationZ;

        public void SetCamera(double fieldOfView, double rotationY, double rotationZ)
        {
            _fieldOfView = fieldOfView;
            _rotationY = rotationY;
            _rotationZ = rotationZ;

            OnCameraChanged();
        }

        /// <summary>
        ///     Lets go of the page, the way the WPF control cleared the Visual of its 3D surface when a
        ///     transition had ended. The point itself is kept: the next transition starts from its view.
        /// </summary>
        public void ReleasePage()
        {
            UnwatchPage();
            SetHoveredControl(null, null);
            DisposeTextureBuffers();

            _playDsPageDrawingCanvas?.Dispose();
            _playDsPageDrawingCanvas = null;

            InvalidateVisual();
        }

        public double GetViewAzimuth()
        {
            return PanoramaUtils.NormalizeAngleInDegrees(_rotationZ + (_panoramaDsPageType?.LeftEdgeAzimuth ?? 0.0));
        }

        public void Show(DsPageDrawing dsPageDrawing, PlayDsPageDrawingCanvas playDsPageDrawingCanvas,
            double? viewAzimuth)
        {
            _panoramaDsPageType = dsPageDrawing.DsPageTypeObject as PanoramaDsPageType;
            if (_panoramaDsPageType is null) return;

            _drawingWidth = dsPageDrawing.Width;
            _drawingHeight = dsPageDrawing.Height;

            PanoramaUtils.CalculateVerticalAngles(_panoramaDsPageType, _drawingWidth, _drawingHeight,
                out _verticalImageAngle, out _upAngle, out _downAngle);

            _padding = GetDrawingCanvasPadding(_drawingWidth, _drawingHeight);

            UnwatchPage();
            SetHoveredControl(null, null);
            _pageChanged = true;

            _playDsPageDrawingCanvas?.Dispose();
            _playDsPageDrawingCanvas = playDsPageDrawingCanvas;
            _pageBackground = dsPageDrawing.ComputeDsPageBackgroundBrush();

            // Painting a part of the page again only works when the background paints the old pixels
            // over; a background that shines through would let the repaints pile up.
            _pageBackgroundIsOpaque = _pageBackground is ISolidColorBrush solidColorBrush &&
                                      solidColorBrush.Color.A == 255 && solidColorBrush.Opacity >= 1.0;
            _fullRedraw = true;
            _dirtyPageRect = null;

            ValidateAndSetFieldOfView(DefaultFieldOfView);
            ValidateAndSetRotationZ((viewAzimuth ?? _panoramaDsPageType.DefaultViewAzimuth) -
                                    _panoramaDsPageType.LeftEdgeAzimuth);
            ValidateAndSetRotationY(0.0);

            // The page keeps changing while it is shown - values, colors, blinking - and the texture
            // has to follow it. The timer of the project is what drives every other live element of a
            // page, so the panorama is repainted with it.
            DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEvent;
            DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEvent;

            RefreshTextureAsync();

            OnCameraChanged();
        }

        /// <summary>
        ///     WPF put the live page visual on the 3D surface and let the pipeline repaint it. Avalonia has
        ///     to hand Skia a finished image, so the page is rendered off screen and the mesh is textured
        ///     with the result. Images of the page load asynchronously, so it is rendered again shortly
        ///     after, when everything of it is in place.
        /// </summary>
        private async void RefreshTextureAsync()
        {
            var canvas = _playDsPageDrawingCanvas;
            if (canvas is null) return;

            foreach (var delayMs in TextureRenderDelaysMs)
            {
                await Task.Delay(delayMs);

                if (_disposed || !ReferenceEquals(canvas, _playDsPageDrawingCanvas)) return;

                // The images of a page arrive one after the other and not every one of them says so
                // with a property; until the page has settled it is drawn as a whole.
                _fullRedraw = true;
                RefreshTexture(canvas);
            }
        }

        private void OnGlobalUITimerEvent(int phase)
        {
            if (_disposed || !IsActive) return;

            var canvas = _playDsPageDrawingCanvas;
            if (canvas is null) return;

            // Visuals that were added to the page since the last tick are taken under watch here, and
            // whatever a change may have missed is caught up with.
            WatchPage(canvas);

            RefreshTextureIfChanged();
        }

        /// <summary>
        ///     A change of the page reaches the texture shortly after it happened, not only at the next
        ///     tick of the half second timer - an animated picture would crawl otherwise. Two renders are
        ///     kept apart by at least the time the last one took, so a page that changes without pause
        ///     cannot eat the thread it is drawn on.
        /// </summary>
        private void ScheduleTextureRefresh()
        {
            if (_disposed || !IsActive || _refreshScheduled) return;

            var interval = Math.Max(MinTextureRefreshIntervalMs, _lastTextureRenderMs * 3);
            var delay = Math.Max(1.0, interval - _sinceTextureRefresh.Elapsed.TotalMilliseconds);

            _refreshScheduled = true;
            DispatcherTimer.RunOnce(() =>
                {
                    _refreshScheduled = false;
                    RefreshTextureIfChanged();
                },
                TimeSpan.FromMilliseconds(delay));
        }

        private void RefreshTextureIfChanged()
        {
            if (_disposed || !IsActive || !_pageChanged) return;

            var canvas = _playDsPageDrawingCanvas;
            if (canvas is null) return;

            // Not while the view is being dragged: repainting the page in the middle of it would make
            // the drag stutter, and nothing of the page can be read at that moment anyway.
            if (_downPoint.HasValue) return;

            // What changed behind the back of the observer waits until he turns to it.
            if (!IsDirtyRectVisible()) return;

            RefreshTexture(canvas);
        }

        /// <summary>
        ///     Whether the part of the page that changed can be seen at all from where the camera looks.
        ///     A panorama page carries its hotspots all around the observer, and painting the texture
        ///     again for one of them behind him would be work that nobody sees.
        /// </summary>
        private bool IsDirtyRectVisible()
        {
            if (_fullRedraw || _dirtyPageRect is null) return true;

            var dirtyRect = _dirtyPageRect.Value;

            // A large area counts as visible: what its border does says nothing about its middle.
            if (dirtyRect.Width * dirtyRect.Height > _drawingWidth * _drawingHeight / 8) return true;

            var bounds = new Rect(Bounds.Size);
            if (bounds.Width <= 0 || bounds.Height <= 0) return true;

            // With room to spare around the viewport, so that a shape which only reaches into the view
            // is not taken for one outside of it.
            bounds = bounds.Inflate(bounds.Width / 4);

            for (var x = 0; x <= 2; x += 1)
            for (var y = 0; y <= 2; y += 1)
            {
                var point = ProjectPagePointToScreen(new Point(dirtyRect.X + dirtyRect.Width * x / 2,
                    dirtyRect.Y + dirtyRect.Height * y / 2));
                if (point is not null && bounds.Contains(point.Value)) return true;
            }

            return false;
        }

        private void RefreshTexture(PlayDsPageDrawingCanvas canvas)
        {
            _pageChanged = false;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var rendered = RenderPageToTexture(canvas);
            _lastTextureRenderMs = stopwatch.Elapsed.TotalMilliseconds;
            _sinceTextureRefresh.Restart();

            if (!rendered) return;

            OnCameraChanged();
        }

        private bool RenderPageToTexture(PlayDsPageDrawingCanvas canvas)
        {
            if (_drawingWidth <= 0 || _drawingHeight <= 0) return false;

            // A whole panorama page is far larger than any texture a graphics card wants; the longest side
            // is capped, which is what the BitmapCache of the WPF control did as well.
            var scale = Math.Min(1.0, MaxTextureSize / Math.Max(_drawingWidth, _drawingHeight));
            var pixelWidth = (int) Math.Round(_drawingWidth * scale);
            var pixelHeight = (int) Math.Round(_drawingHeight * scale);
            if (pixelWidth <= 0 || pixelHeight <= 0) return false;

            try
            {
                var pixelSize = new PixelSize(pixelWidth, pixelHeight);
                if (_renderTargetBitmap is null || _renderTargetBitmap.PixelSize != pixelSize)
                {
                    _renderTargetBitmap?.Dispose();
                    _renderTargetBitmap = new RenderTargetBitmap(pixelSize, new Avalonia.Vector(96, 96));
                    _texture.SetInfo(new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888,
                        SKAlphaType.Premul));
                    _fullRedraw = true;
                }

                var renderTargetBitmap = _renderTargetBitmap;

                // Only what changed is painted again. A whole page costs more than a hundred
                // milliseconds, and what changes between two ticks is usually one blinking shape.
                var pageRect = new Rect(0, 0, _drawingWidth, _drawingHeight);
                var clipRect = pageRect;
                var clear = true;

                if (!_fullRedraw && _dirtyPageRect.HasValue && _pageBackgroundIsOpaque)
                {
                    clipRect = _dirtyPageRect.Value.Intersect(pageRect);
                    clear = false;

                    if (clipRect.Width <= 0 || clipRect.Height <= 0)
                    {
                        _dirtyPageRect = null;
                        return false;
                    }
                }

                using (var drawingContext = renderTargetBitmap.CreateDrawingContext(clear))
                using (drawingContext.PushTransform(Matrix.CreateScale(scale, scale)))
                using (drawingContext.PushClip(clipRect))
                {
                    if (_pageBackground is not null)
                        drawingContext.FillRectangle(_pageBackground, clipRect);

                    canvas.Render(drawingContext);
                    RenderChildren(canvas, drawingContext, clipRect);
                }

                _fullRedraw = false;
                _dirtyPageRect = null;

                // Only the piece of the picture that was painted again is sent to the texture; sending
                // the whole page would upload every pixel of it once more.
                var patchRect = GetPatchRect(clipRect, scale, pixelSize);
                if (patchRect.Width <= 0 || patchRect.Height <= 0) return false;

                var patchBitmap = new SKBitmap(new SKImageInfo(patchRect.Width, patchRect.Height,
                    SKColorType.Bgra8888, SKAlphaType.Premul));
                renderTargetBitmap.CopyPixels(patchRect, patchBitmap.GetPixels(), patchBitmap.ByteCount,
                    patchBitmap.RowBytes);

                _texture.AddPatch(patchBitmap,
                    new SKRect(patchRect.X, patchRect.Y, patchRect.Right, patchRect.Bottom));

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     The pixels of the texture the given part of the page covers, taken with a whole pixel to
        ///     spare on every side.
        /// </summary>
        private static PixelRect GetPatchRect(Rect pageRect, double scale, PixelSize pixelSize)
        {
            var left = Math.Max(0, (int) Math.Floor(pageRect.X * scale));
            var top = Math.Max(0, (int) Math.Floor(pageRect.Y * scale));
            var right = Math.Min(pixelSize.Width, (int) Math.Ceiling(pageRect.Right * scale) + 1);
            var bottom = Math.Min(pixelSize.Height, (int) Math.Ceiling(pageRect.Bottom * scale) + 1);

            return new PixelRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
        }

        /// <summary>
        ///     The size of the page plus the margins the WPF control padded it with, which is the area the
        ///     mesh is textured with.
        /// </summary>
        public Size PaddedPageSize => new(
            _drawingWidth + _padding.Left + _padding.Right,
            _drawingHeight + _padding.Top + _padding.Bottom);

        /// <summary>
        ///     Where a point of the page shows up on the screen, or null when it is behind the camera or
        ///     off the viewport.
        /// </summary>
        public Point? ProjectPagePointToScreen(Point pagePoint)
        {
            if (_panoramaDsPageType is null) return null;

            var paddedSize = PaddedPageSize;
            if (paddedSize.Width <= 0 || paddedSize.Height <= 0) return null;

            var u = (pagePoint.X + _padding.Left) / paddedSize.Width;
            var v = (pagePoint.Y + _padding.Top) / paddedSize.Height;

            var direction = PanoramaMesh.GetDirection(_panoramaDsPageType.PanoramaType, u, v);
            return Project(Vector3.Transform(direction, GetCameraMatrix()), Bounds.Size);
        }

        /// <summary>
        ///     Where a point of the screen lands on the page, or null when it misses it.
        ///     The inverse of ProjectPagePointToScreen. WPF needed nothing of the kind: the page was a
        ///     live visual on the 3D surface and the hit test of the Viewport3D routed the mouse to it.
        /// </summary>
        public Point? ProjectScreenPointToPage(Point screenPoint)
        {
            if (_panoramaDsPageType is null) return null;

            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0) return null;

            var halfWidthAtUnitDepth = Math.Tan(PanoramaUtils.ConvertToRadians(_fieldOfView) / 2);
            var scale = size.Width / 2 / halfWidthAtUnitDepth;

            var horizontal = (screenPoint.X - size.Width / 2) / scale;
            var vertical = -(screenPoint.Y - size.Height / 2) / scale;

            // What ProjectUnchecked does, read backwards, with the depth taken as 1.
            var viewDirection = new Vector3(1, (float) -horizontal, (float) vertical);

            // The camera matrix is a rotation, so its transpose takes the direction back to the model.
            var direction = Vector3.Transform(viewDirection, Matrix4x4.Transpose(GetCameraMatrix()));

            if (!PanoramaMesh.TryGetPagePoint(_panoramaDsPageType.PanoramaType, direction, out var u, out var v))
                return null;

            var paddedSize = PaddedPageSize;
            if (paddedSize.Width <= 0 || paddedSize.Height <= 0) return null;

            var pagePoint = new Point(u * paddedSize.Width - _padding.Left,
                v * paddedSize.Height - _padding.Top);

            if (pagePoint.X < 0 || pagePoint.Y < 0 ||
                pagePoint.X > _drawingWidth || pagePoint.Y > _drawingHeight) return null;

            return pagePoint;
        }

        /// <summary>
        ///     How much a page is magnified around the given point, used to scale the shapes of the page so
        ///     that they keep the size the panorama itself is drawn with.
        /// </summary>
        public double GetScaleAt(Point pagePoint)
        {
            var paddedSize = PaddedPageSize;
            if (paddedSize.Width <= 0) return 1.0;

            const double delta = 8.0;
            var p0 = ProjectPagePointToScreen(new Point(pagePoint.X - delta, pagePoint.Y));
            var p1 = ProjectPagePointToScreen(new Point(pagePoint.X + delta, pagePoint.Y));
            if (p0 is null || p1 is null) return 1.0;

            var scale = Math.Abs(p1.Value.X - p0.Value.X) / (2 * delta);
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0) return 1.0;
            return scale;
        }

        public void ValidateAndSetFieldOfView(double value)
        {
            if (value > FieldOfViewMax) value = FieldOfViewMax;
            else if (value < FieldOfViewMin) value = FieldOfViewMin;

            if (Bounds.Height > 0)
            {
                var horizontalFieldOfViewTanMax = Math.Tan((_upAngle - _downAngle) * Math.PI / 360) *
                                                  Bounds.Width / Bounds.Height;
                var horizontalFieldOfViewAngleMax = Math.Atan(horizontalFieldOfViewTanMax) * 360 / Math.PI;
                if (value > horizontalFieldOfViewAngleMax) value = horizontalFieldOfViewAngleMax;
            }

            _fieldOfView = value;
        }

        public void ValidateAndSetRotationY(double value)
        {
            // The camera may not look further up or down than the image reaches.
            var verticalFieldOfViewTan = Math.Tan(_fieldOfView * Math.PI / 360);
            if (Bounds.Width > 0)
                verticalFieldOfViewTan *= Bounds.Height / Bounds.Width;
            var verticalFieldOfViewAngle = Math.Atan(verticalFieldOfViewTan) * 360 / Math.PI;

            var max = _upAngle - verticalFieldOfViewAngle / 2;
            var min = _downAngle + verticalFieldOfViewAngle / 2;
            if (min > max) min = max = (min + max) / 2;

            if (value > max) value = max;
            else if (value < min) value = min;

            _rotationY = value;
        }

        public void ValidateAndSetRotationZ(double value)
        {
            _rotationZ = PanoramaUtils.NormalizeAngleInDegrees(value);
        }

        public void OnPointerDown(Point position)
        {
            _downPoint = position;
            _rotationVectorX = _rotationY;
            _rotationVectorY = _rotationZ;
        }

        public void OnPointerUp()
        {
            _downPoint = null;
        }

        public void OnPointerMoved(Point position)
        {
            if (!_downPoint.HasValue) return;

            var offsetX = (position.X - _downPoint.Value.X) * 0.25;
            var offsetY = (position.Y - _downPoint.Value.Y) * 0.25;

            ValidateAndSetRotationZ(_rotationVectorY - offsetX);
            ValidateAndSetRotationY(_rotationVectorX + offsetY);

            OnCameraChanged();
        }

        public void OnWheel(double delta)
        {
            ValidateAndSetFieldOfView(_fieldOfView - delta * 5);
            ValidateAndSetRotationY(_rotationY);

            OnCameraChanged();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (!_texture.HasContent || _panoramaDsPageType is null) return;
            if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

            var mesh = PanoramaMesh.Get(_panoramaDsPageType.PanoramaType);
            var (vertices, textures) = BuildTriangles(mesh, Bounds.Size);
            if (vertices.Length == 0) return;

            context.Custom(new PanoramaDrawOperation(new Rect(Bounds.Size), _texture, vertices, textures));
        }

        #endregion

        #region protected functions

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!IsActive) return;

            var position = e.GetPosition(this);
            _pressedPoint = position;
            _dragged = false;

            OnPointerDown(position);
            e.Pointer.Capture(this);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (!IsActive) return;

            var position = e.GetPosition(this);

            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                UpdateHover(position, e);
                return;
            }

            if (_pressedPoint.HasValue && !_dragged &&
                (Math.Abs(position.X - _pressedPoint.Value.X) > ClickToleranceInPixels ||
                 Math.Abs(position.Y - _pressedPoint.Value.Y) > ClickToleranceInPixels))
                _dragged = true;

            OnPointerMoved(position);
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);

            if (!IsActive) return;

            OnPointerUp();
            e.Pointer.Capture(null);

            // A press that did not turn into a drag is a click on the page behind the panorama.
            if (!_dragged && _pressedPoint.HasValue && e.InitialPressMouseButton == MouseButton.Left)
                ClickAt(e.GetPosition(this));

            _pressedPoint = null;
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);

            Cursor = ArrowCursor;
            SetHoveredControl(null, e);
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (!IsActive) return;

            OnWheel(e.Delta.Y);
            e.Handled = true;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            // The clamps depend on the aspect ratio of the viewport, as they did in WPF's OnSizeChanged.
            ValidateAndSetFieldOfView(_fieldOfView);
            ValidateAndSetRotationY(_rotationY);
            OnCameraChanged();

            return result;
        }

        #endregion

        #region private functions

        private void OnCameraChanged()
        {
            InvalidateVisual();
            CameraChanged?.Invoke();

            // Something that changed out of sight may have come into it now.
            if (_pageChanged) ScheduleTextureRefresh();
        }

        /// <summary>
        ///     Rotates the model the way the WPF control did: Quaternion(AxisY, rotationY) *
        ///     Quaternion(AxisZ, rotationZ), that is the azimuth first and the elevation after it.
        /// </summary>
        private Matrix4x4 GetCameraMatrix()
        {
            var mz = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 0, 1),
                (float) PanoramaUtils.ConvertToRadians(_rotationZ));
            var my = Matrix4x4.CreateFromAxisAngle(new Vector3(0, 1, 0),
                (float) PanoramaUtils.ConvertToRadians(_rotationY));
            return mz * my;
        }

        /// <summary>
        ///     The camera of the WPF control sits in the origin, looks along +X and has +Z up, and its
        ///     FieldOfView is the horizontal one.
        /// </summary>
        private Point? Project(Vector3 p, Size size)
        {
            if (p.X <= NearPlane) return null;
            return ProjectUnchecked(p, size);
        }

        private Point ProjectUnchecked(Vector3 p, Size size)
        {
            var halfWidthAtUnitDepth = Math.Tan(PanoramaUtils.ConvertToRadians(_fieldOfView) / 2);
            var scale = size.Width / 2 / halfWidthAtUnitDepth;

            var horizontal = -p.Y / p.X;
            var vertical = p.Z / p.X;

            return new Point(size.Width / 2 + horizontal * scale, size.Height / 2 - vertical * scale);
        }

        /// <summary>
        ///     Projects the mesh and clips it against the near plane, so that the half of the sphere behind
        ///     the camera does not fold over the visible one.
        /// </summary>
        private (SKPoint[], SKPoint[]) BuildTriangles(PanoramaMesh mesh, Size size)
        {
            var matrix = GetCameraMatrix();

            var count = mesh.Positions.Length;
            var viewPositions = new Vector3[count];
            for (var i = 0; i < count; i += 1)
                viewPositions[i] = Vector3.Transform(mesh.Positions[i], matrix);

            var textureRect = GetTextureRect();

            var vertices = new List<SKPoint>(mesh.TriangleIndices.Length);
            var textures = new List<SKPoint>(mesh.TriangleIndices.Length);

            var clippedPositions = new Vector3[4];
            var clippedTextures = new Vector2[4];

            for (var i = 0; i < mesh.TriangleIndices.Length; i += 3)
            {
                var i0 = mesh.TriangleIndices[i];
                var i1 = mesh.TriangleIndices[i + 1];
                var i2 = mesh.TriangleIndices[i + 2];

                var clippedCount = ClipTriangleAgainstNearPlane(
                    viewPositions[i0], viewPositions[i1], viewPositions[i2],
                    mesh.TextureCoordinates[i0], mesh.TextureCoordinates[i1], mesh.TextureCoordinates[i2],
                    clippedPositions, clippedTextures);

                for (var k = 1; k + 1 < clippedCount; k += 1)
                {
                    AddVertex(vertices, textures, clippedPositions[0], clippedTextures[0], size, textureRect);
                    AddVertex(vertices, textures, clippedPositions[k], clippedTextures[k], size, textureRect);
                    AddVertex(vertices, textures, clippedPositions[k + 1], clippedTextures[k + 1], size, textureRect);
                }
            }

            return (vertices.ToArray(), textures.ToArray());
        }

        private void AddVertex(List<SKPoint> vertices, List<SKPoint> textures, Vector3 position, Vector2 texture,
            Size size, Rect textureRect)
        {
            var screen = ProjectUnchecked(position, size);
            vertices.Add(new SKPoint((float) screen.X, (float) screen.Y));
            textures.Add(new SKPoint(
                (float) (textureRect.X + texture.X * textureRect.Width),
                (float) (textureRect.Y + texture.Y * textureRect.Height)));
        }

        /// <summary>
        ///     Redrawing a whole panorama page costs more than a hundred milliseconds, so it is only done
        ///     when the page really changed. Every visual of the page is watched, and with it every brush
        ///     it paints with: a blinking brush changes its own color, not a property of the shape.
        /// </summary>
        private void WatchPage(Visual visual)
        {
            if (_watchedObjects.Add(visual))
                visual.PropertyChanged += OnWatchedPropertyChanged;

            foreach (var property in BrushProperties)
            {
                if (!AvaloniaPropertyRegistry.Instance.IsRegistered(visual, property)) continue;
                if (visual.GetValue(property) is not AvaloniaObject brush) continue;

                if (_watchedObjects.Add(brush))
                    brush.PropertyChanged += OnWatchedPropertyChanged;

                // A brush is shared by the shapes that paint with it, and a change of it makes every
                // one of them stale.
                if (!_brushUsers.TryGetValue(brush, out var users))
                    _brushUsers.Add(brush, users = new List<Visual>());
                if (!users.Contains(visual)) users.Add(visual);
            }

            foreach (var child in visual.GetVisualChildren())
                WatchPage(child);
        }

        private void UnwatchPage()
        {
            foreach (var watchedObject in _watchedObjects)
                watchedObject.PropertyChanged -= OnWatchedPropertyChanged;
            _watchedObjects.Clear();
            _brushUsers.Clear();
        }

        private void OnWatchedPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            _pageChanged = true;

            switch (sender)
            {
                case Visual visual:
                    // A visual that moved leaves its old place behind, and that has to be painted over.
                    if (e.Property == Visual.BoundsProperty && e.OldValue is Rect oldBounds &&
                        visual.GetVisualParent() is { } parent)
                        AddDirtyRect(parent, oldBounds);

                    AddDirtyVisual(visual);
                    break;
                case AvaloniaObject watchedObject when _brushUsers.TryGetValue(watchedObject, out var users):
                    foreach (var user in users)
                        AddDirtyVisual(user);
                    break;
                default:
                    _fullRedraw = true;
                    break;
            }

            ScheduleTextureRefresh();
        }

        private void AddDirtyVisual(Visual visual)
        {
            // A shape is painted again as a whole: what changed is one part of it, and the parts of a
            // shape reach beyond their own boxes.
            var target = visual;
            var canvas = _playDsPageDrawingCanvas;
            var ancestor = visual;
            while (ancestor is not null && !ReferenceEquals(ancestor, canvas))
            {
                if (ancestor is DsShapeViewBase)
                {
                    target = ancestor;
                    break;
                }

                ancestor = ancestor.GetVisualParent();
            }

            AddDirtyRect(target, new Rect(target.Bounds.Size));
        }

        private void AddDirtyRect(Visual visual, Rect localRect)
        {
            var canvas = _playDsPageDrawingCanvas;
            if (canvas is null) return;

            var pageRect = GetPageRect(visual, canvas, localRect);
            if (pageRect is null)
            {
                _fullRedraw = true;
                return;
            }

            var dirtyRect = pageRect.Value.Inflate(DirtyRectMargin);
            _dirtyPageRect = _dirtyPageRect is null ? dirtyRect : _dirtyPageRect.Value.Union(dirtyRect);
        }

        private static Rect? GetPageRect(Visual visual, Visual canvas, Rect localRect)
        {
            var topLeft = visual.TranslatePoint(localRect.TopLeft, canvas);
            var topRight = visual.TranslatePoint(localRect.TopRight, canvas);
            var bottomLeft = visual.TranslatePoint(localRect.BottomLeft, canvas);
            var bottomRight = visual.TranslatePoint(localRect.BottomRight, canvas);
            if (topLeft is null || topRight is null || bottomLeft is null || bottomRight is null) return null;

            var minX = Math.Min(Math.Min(topLeft.Value.X, topRight.Value.X),
                Math.Min(bottomLeft.Value.X, bottomRight.Value.X));
            var maxX = Math.Max(Math.Max(topLeft.Value.X, topRight.Value.X),
                Math.Max(bottomLeft.Value.X, bottomRight.Value.X));
            var minY = Math.Min(Math.Min(topLeft.Value.Y, topRight.Value.Y),
                Math.Min(bottomLeft.Value.Y, bottomRight.Value.Y));
            var maxY = Math.Max(Math.Max(topLeft.Value.Y, topRight.Value.Y),
                Math.Max(bottomLeft.Value.Y, bottomRight.Value.Y));

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        ///     Sends the click to the button of the page it landed on. The page is drawn as a texture and
        ///     its host has no size, so it takes no part in the hit test of the tree and is searched here.
        /// </summary>
        private void ClickAt(Point screenPoint)
        {
            var canvas = _playDsPageDrawingCanvas;
            if (canvas is null) return;

            var pagePoint = ProjectScreenPointToPage(screenPoint);
            if (pagePoint is null) return;

            var button = FindButtonAt(canvas, pagePoint.Value);
            button?.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        ///     A hand over a button of the page and the button knowing that the pointer is on it, both
        ///     of which the hit test of the WPF viewport gave for free: there the mouse really did reach
        ///     the page. Here the page is only a texture, so the state is set by hand - and with it the
        ///     frame its style draws around a hotspot.
        /// </summary>
        private void UpdateHover(Point screenPoint, PointerEventArgs? e)
        {
            var canvas = _playDsPageDrawingCanvas;
            if (canvas is null) return;

            var pagePoint = ProjectScreenPointToPage(screenPoint);
            var button = pagePoint is null ? null : FindButtonAt(canvas, pagePoint.Value);

            Cursor = button is not null ? HandCursor : ArrowCursor;

            SetHoveredControl(button, e);
        }

        private void SetHoveredControl(Control? control, PointerEventArgs? e)
        {
            if (ReferenceEquals(control, _hoveredControl)) return;

            var previouslyHoveredControl = _hoveredControl;
            _hoveredControl = control;

            if (previouslyHoveredControl is not null)
                SetPointerOver(previouslyHoveredControl, false, e);

            if (control is not null)
                SetPointerOver(control, true, e);
        }

        private void SetPointerOver(Control control, bool value, PointerEventArgs? e)
        {
            // In WPF the whole line of parents of the shape knew about the mouse as well, and a style
            // may well be written against one of them.
            for (Visual? visual = control;
                 visual is not null && !ReferenceEquals(visual, _playDsPageDrawingCanvas);
                 visual = visual.GetVisualParent())
                if (visual is InputElement inputElement)
                    try
                    {
                        inputElement.SetValue(InputElement.IsPointerOverProperty, value);
                    }
                    catch (Exception)
                    {
                        // The state is a nicety; a theme that will not have it set is no reason to stop.
                    }

            if (e is null) return;

            // The commands a shape carries for the pointer entering and leaving it run off these.
            try
            {
                control.RaiseEvent(new PointerEventArgs(
                    value ? InputElement.PointerEnteredEvent : InputElement.PointerExitedEvent,
                    control, e.Pointer, this, e.GetPosition(this), e.Timestamp,
                    e.GetCurrentPoint(this).Properties, e.KeyModifiers));
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///     The topmost button of the page under the given point of it. The tree is walked the way
        ///     RenderChildren draws it, so a shape is hit where its pixels are.
        /// </summary>
        private static Button? FindButtonAt(Visual visual, Point point)
        {
            var children = visual.GetVisualChildren().ToArray();

            for (var i = children.Length - 1; i >= 0; i -= 1)
            {
                var child = children[i];
                if (child is Control { IsVisible: false }) continue;

                var childPoint = new Point(point.X - child.Bounds.X, point.Y - child.Bounds.Y);

                if (child.RenderTransform is not null)
                {
                    var origin = child.RenderTransformOrigin.ToPixels(child.Bounds.Size);
                    var transform = Matrix.CreateTranslation(-origin.X, -origin.Y) *
                                    child.RenderTransform.Value *
                                    Matrix.CreateTranslation(origin.X, origin.Y);
                    if (!transform.TryInvert(out var inverted)) continue;
                    childPoint = childPoint.Transform(inverted);
                }

                // A visual without a size of its own is passed through: only what is drawn has to
                // contain the point.
                if (child.Bounds.Width > 0 && child.Bounds.Height > 0 &&
                    !new Rect(child.Bounds.Size).Contains(childPoint)) continue;

                var button = FindButtonAt(child, childPoint) ??
                             (child is Button { IsEffectivelyEnabled: true } b ? b : null);
                if (button is not null) return button;
            }

            return null;
        }

        /// <summary>
        ///     The page lives in a zero sized host, so it is laid out and its shapes are alive, but nothing
        ///     of it reaches the screen. RenderTargetBitmap.Render would clip it away with the host, so the
        ///     tree is walked and every visual is drawn at the place its layout gave it.
        /// </summary>
        private static void RenderChildren(Visual visual, DrawingContext drawingContext, Rect clipRect)
        {
            foreach (var child in visual.GetVisualChildren())
            {
                if (child is Control { IsVisible: false }) continue;

                var bounds = child.Bounds;
                var childClipRect = clipRect.Translate(new Avalonia.Vector(-bounds.X, -bounds.Y));

                using (drawingContext.PushTransform(Matrix.CreateTranslation(bounds.X, bounds.Y)))
                {
                    if (child.RenderTransform is not null)
                    {
                        var origin = child.RenderTransformOrigin.ToPixels(bounds.Size);
                        var transform = Matrix.CreateTranslation(-origin.X, -origin.Y) *
                                        child.RenderTransform.Value *
                                        Matrix.CreateTranslation(origin.X, origin.Y);
                        if (transform.TryInvert(out var inverted))
                            childClipRect = childClipRect.TransformToAABB(inverted);

                        using (drawingContext.PushTransform(transform))
                        {
                            child.Render(drawingContext);
                            RenderChildren(child, drawingContext, childClipRect);
                        }
                    }
                    else
                    {
                        // Nothing outside the clip has to be drawn at all - that is what makes a partial
                        // repaint cheap. A shape may paint a little beyond its box, so the box is taken
                        // with a margin.
                        if (bounds.Width > 0 && bounds.Height > 0 &&
                            !new Rect(bounds.Size).Inflate(DirtyRectMargin).Intersects(childClipRect))
                            continue;

                        child.Render(drawingContext);
                        RenderChildren(child, drawingContext, childClipRect);
                    }
                }
            }
        }

        /// <summary>
        ///     Where the page image sits inside the padded page, in pixels of the texture.
        /// </summary>
        private Rect GetTextureRect()
        {
            var info = _texture.Info;
            if (info.Width <= 0 || info.Height <= 0) return new Rect(0, 0, 1, 1);

            var paddedSize = PaddedPageSize;
            if (paddedSize.Width <= 0 || paddedSize.Height <= 0)
                return new Rect(0, 0, info.Width, info.Height);

            // A normalized texture coordinate addresses the padded page; the image itself covers only the
            // part of it the margins leave over.
            var width = info.Width * paddedSize.Width / _drawingWidth;
            var height = info.Height * paddedSize.Height / _drawingHeight;
            var x = -_padding.Left / _drawingWidth * info.Width;
            var y = -_padding.Top / _drawingHeight * info.Height;
            return new Rect(x, y, width, height);
        }

        private static int ClipTriangleAgainstNearPlane(Vector3 p0, Vector3 p1, Vector3 p2,
            Vector2 t0, Vector2 t1, Vector2 t2, Vector3[] outPositions, Vector2[] outTextures)
        {
            Span<Vector3> positions = stackalloc Vector3[3] { p0, p1, p2 };
            Span<Vector2> texts = stackalloc Vector2[3] { t0, t1, t2 };

            var count = 0;
            for (var i = 0; i < 3; i += 1)
            {
                var current = positions[i];
                var next = positions[(i + 1) % 3];
                var currentInside = current.X > NearPlane;
                var nextInside = next.X > NearPlane;

                if (currentInside)
                {
                    if (count == 4) break;
                    outPositions[count] = current;
                    outTextures[count] = texts[i];
                    count += 1;
                }

                if (currentInside != nextInside)
                {
                    var k = (NearPlane - current.X) / (next.X - current.X);
                    if (count == 4) break;
                    outPositions[count] = Vector3.Lerp(current, next, k);
                    outTextures[count] = Vector2.Lerp(texts[i], texts[(i + 1) % 3], k);
                    count += 1;
                }
            }

            return count;
        }

        /// <summary>
        ///     Port of PanoramaViewport3D.GetDrawingCanvasPadding: how much empty space around the page the
        ///     mesh is textured with, when the image does not cover the whole sphere or cylinder.
        /// </summary>
        private Thickness GetDrawingCanvasPadding(double drawingWidth, double drawingHeight)
        {
            double topMargin = 0;
            double bottomMargin = 0;
            double leftMargin = 0;
            double rightMargin = 0;

            if (_panoramaDsPageType is null) return new Thickness(0);

            switch (_panoramaDsPageType.PanoramaType)
            {
                case PanoramaType.Cylindrical:
                {
                    if (_upAngle >= PanoramaMesh.CylinderUpDownAngle)
                    {
                        _downAngle = _downAngle - _upAngle + PanoramaMesh.CylinderUpDownAngle;
                        _upAngle = PanoramaMesh.CylinderUpDownAngle;
                    }

                    if (_downAngle <= -PanoramaMesh.CylinderUpDownAngle)
                    {
                        _upAngle = _upAngle - _downAngle - PanoramaMesh.CylinderUpDownAngle;
                        _downAngle = -PanoramaMesh.CylinderUpDownAngle;
                    }

                    var upImageSize = Math.Tan(_upAngle * Math.PI / 180.0);
                    var downImageSize = Math.Tan(-_downAngle * Math.PI / 180.0);
                    var verticalImageSize = upImageSize + downImageSize;
                    var cylinderUpDownSize = Math.Tan(PanoramaMesh.CylinderUpDownAngle * Math.PI / 180.0);
                    topMargin = drawingHeight * (cylinderUpDownSize - upImageSize) / verticalImageSize;
                    bottomMargin = drawingHeight * (cylinderUpDownSize - downImageSize) / verticalImageSize;

                    var width360 = drawingWidth * 360.0 / _panoramaDsPageType.HorizontalImageAngle;
                    leftMargin = rightMargin = (width360 - drawingWidth) / 2.0;
                }
                    break;
                case PanoramaType.Spherical:
                {
                    topMargin = drawingHeight * (90.0 - _upAngle) / _verticalImageAngle;
                    bottomMargin = drawingHeight * (90.0 + _downAngle) / _verticalImageAngle;

                    var width360 = drawingWidth * 360.0 / _panoramaDsPageType.HorizontalImageAngle;
                    leftMargin = rightMargin = (width360 - drawingWidth) / 2.0;
                }
                    break;
            }

            if (topMargin < 0) topMargin = 0;
            if (bottomMargin < 0) bottomMargin = 0;

            return new Thickness(leftMargin, topMargin, rightMargin, bottomMargin);
        }

        private void DisposeTextureBuffers()
        {
            _texture.Clear();

            _renderTargetBitmap?.Dispose();
            _renderTargetBitmap = null;
        }

        #endregion

        #region private fields

        private const double DefaultFieldOfView = 100;
        private const double FieldOfViewMin = 50;
        private const double FieldOfViewMax = 110;
        private const float NearPlane = 0.01f;
        private const double MaxTextureSize = 4096;
        private const double ClickToleranceInPixels = 4;
        private const double DirtyRectMargin = 16;
        private const double MinTextureRefreshIntervalMs = 100;

        /// <summary>
        ///     The properties a shape of a page paints itself with; the brushes behind them are watched
        ///     for changes of their own.
        /// </summary>
        private static readonly AvaloniaProperty[] BrushProperties =
        {
            Shape.FillProperty, Shape.StrokeProperty,
            TemplatedControl.BackgroundProperty, TemplatedControl.BorderBrushProperty,
            TemplatedControl.ForegroundProperty,
            TextBlock.BackgroundProperty, TextBlock.ForegroundProperty,
            Border.BackgroundProperty, Border.BorderBrushProperty,
            Panel.BackgroundProperty
        };

        private static readonly Cursor HandCursor = new(StandardCursorType.Hand);
        private static readonly Cursor ArrowCursor = new(StandardCursorType.Arrow);
        private static readonly int[] TextureRenderDelaysMs = { 50, 500 };

        private PanoramaDsPageType? _panoramaDsPageType;

        private readonly PanoramaTexture _texture = new();
        private RenderTargetBitmap? _renderTargetBitmap;
        private PlayDsPageDrawingCanvas? _playDsPageDrawingCanvas;
        private IBrush? _pageBackground;
        private readonly HashSet<AvaloniaObject> _watchedObjects = new();
        private readonly Dictionary<AvaloniaObject, List<Visual>> _brushUsers = new();
        private bool _pageChanged;
        private bool _refreshScheduled;
        private double _lastTextureRenderMs;
        private readonly System.Diagnostics.Stopwatch _sinceTextureRefresh = System.Diagnostics.Stopwatch.StartNew();
        private bool _fullRedraw = true;
        private bool _pageBackgroundIsOpaque;
        private Rect? _dirtyPageRect;

        private double _drawingWidth;
        private double _drawingHeight;
        private Thickness _padding;

        private double _fieldOfView = DefaultFieldOfView;
        private double _rotationY;
        private double _rotationZ;
        private double _upAngle;
        private double _downAngle;
        private double _verticalImageAngle;

        private Control? _hoveredControl;
        private Point? _downPoint;
        private Point? _pressedPoint;
        private bool _dragged;
        private double _rotationVectorX;
        private double _rotationVectorY;

        private bool _disposed;

        #endregion

        #region private classes

        private sealed class PanoramaDrawOperation : ICustomDrawOperation
        {
            public PanoramaDrawOperation(Rect bounds, PanoramaTexture texture, SKPoint[] vertices,
                SKPoint[] textures)
            {
                Bounds = bounds;
                _texture = texture;
                _vertices = vertices;
                _textures = textures;
            }

            public Rect Bounds { get; }

            public void Dispose()
            {
            }

            public bool Equals(ICustomDrawOperation? other) => false;

            public bool HitTest(Point p) => Bounds.Contains(p);

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null) return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                var image = _texture.GetImage(lease.GrContext);
                if (image is null) return;

                canvas.Save();
                canvas.ClipRect(new SKRect(0, 0, (float) Bounds.Width, (float) Bounds.Height));

                using var paint = new SKPaint();

                // The mesh covers the whole viewport, so its triangles have no edges to smooth - but
                // the picture on it is stretched, and reading it point by point is what leaves the
                // steps on every line of the page.
                paint.IsAntialias = false;
                using var shader = image.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp,
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
                paint.Shader = shader;

                canvas.DrawVertices(SKVertexMode.Triangles, _vertices, _textures, null, paint);

                canvas.Restore();
            }

            private readonly PanoramaTexture _texture;
            private readonly SKPoint[] _vertices;
            private readonly SKPoint[] _textures;
        }

        #endregion
    }
}
