using System;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering.Composition;
using Avalonia.Skia;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Ssz.Utils.Avalonia.Model3D;

public partial class Model3DControl : UserControl
{
    #region construction and destruction

    public Model3DControl()
    {
        InitializeComponent();

        Viewport.AttachedToVisualTree += ViewportAttachedToVisualTree;
        Viewport.DetachedFromVisualTree += ViewportDetachedFromVisualTree;

        Border.PointerPressed += OnPointerPressed;
        Border.PointerMoved += OnPointerMoved;
        Border.PointerReleased += OnPointerReleased;
        Border.PointerWheelChanged += OnPointerWheelChanged;

        PropertyChanged += OnPropertyChanged;
    }

    #endregion

    #region public functions

    public static readonly AvaloniaProperty<Model3DScene?> DataProperty = AvaloniaProperty.Register<Model3DControl, Model3DScene?>(
        nameof(Data));

    public Model3DScene? Data
    {
        get => GetValue(DataProperty) as Model3DScene;
        set => SetValue(DataProperty, value);
    }

    public void ResetCamera()
    {
        FitViewToScene(Data);
        SendCurrentState(withScene: false);
    }

    #endregion

    #region protected functions

    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        UpdateVisualSize(size);
        return size;
    }

    #endregion

    #region private functions

    private void ViewportAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var visual = ElementComposition.GetElementVisual(Viewport);
        if (visual == null)
            return;

        _visual = visual.Compositor.CreateCustomVisual(new GlVisual(new OpenGlContent()));
        ElementComposition.SetElementChildVisual(Viewport, _visual);
        UpdateVisualSize(Bounds.Size);

        OnDataPropertyChanged();
    }

    private void ViewportDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _visual?.SendHandlerMessage(new DisposeMessage());
        _visual = null;
        ElementComposition.SetElementChildVisual(Viewport, null);
    }

    private void UpdateVisualSize(Size size)
    {
        if (_visual != null)
            _visual.Size = new global::Avalonia.Vector(size.Width, size.Height);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastMousePos = e.GetPosition(Border);

        var p = e.GetCurrentPoint(Border).Properties;        
        if (p.IsLeftButtonPressed || p.IsMiddleButtonPressed)
            _dragMode = DragMode.Pan;
        else if (p.IsRightButtonPressed)
            _dragMode = DragMode.Rotate;
        else
            _dragMode = DragMode.None;

        if (_dragMode != DragMode.None)
            e.Pointer.Capture(Border);

        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _dragMode = DragMode.None;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragMode == DragMode.None)
            return;

        var currentPos = e.GetPosition(Border);
        var delta = currentPos - _lastMousePos;
        _lastMousePos = currentPos;

        switch (_dragMode)
        {
            case DragMode.Rotate:
                _yaw -= (float)delta.X * RotateSensitivity;
                _pitch += (float)delta.Y * RotateSensitivity;
                _pitch = Math.Clamp(_pitch, -MaxPitch, MaxPitch);
                break;

            case DragMode.Pan:
                {
                    var (forward, right, up) = GetCameraBasis();
                    var scale = _distance * PanSensitivity;

                    _target += (-right * (float)delta.X * scale);
                    _target += (up * (float)delta.Y * scale);
                    break;
                }
        }

        SendCurrentState();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _distance *= MathF.Exp(-(float)e.Delta.Y * ZoomSensitivity);
        _distance = Math.Clamp(_distance, 0.05f, 5000f);

        SendCurrentState();
        e.Handled = true;
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == DataProperty)
            OnDataPropertyChanged();
    }

    private void OnDataPropertyChanged()
    {
        if (!_cameraInitialized)
        {
            FitViewToScene(Data);
            _cameraInitialized = true;
        }

        // Передаём новую сцену, но положение камеры не трогаем
        _visual?.SendHandlerMessage(new Model3DMessage
        {
            Model3DScene = Data,
            Target = _target,
            Yaw = _yaw,
            Pitch = _pitch,
            Distance = _distance,
        });
    }

    private void SendCurrentState(bool withScene = false)
    {
        _visual?.SendHandlerMessage(new Model3DMessage
        {
            Model3DScene = withScene ? Data : null,
            Target = _target,
            Yaw = _yaw,
            Pitch = _pitch,
            Distance = _distance,
        });
    }

    private void FitViewToScene(Model3DScene? scene)
    {
        if (scene is null)
            return;

        bool hasAny = false;
        var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        void Include(Vector3 p)
        {
            min = Vector3.Min(min, p);
            max = Vector3.Max(max, p);
            hasAny = true;
        }

        if (scene.Points is not null)
        {
            foreach (var point in scene.Points)
                Include(point.Position);
        }

        if (scene.Lines is not null)
        {
            foreach (var polyline in scene.Lines)
            {
                if (polyline is null)
                    continue;

                foreach (var point in polyline)
                    Include(point.Position);
            }
        }

        if (!hasAny)
            return;

        _target = (min + max) * 0.5f;

        var extent = max - min;
        var radius = MathF.Max(extent.Length() * 0.5f, 1f);

        // Стартовый вид как в примере Helix: камера стоит на +X и смотрит в центр.
        _yaw = MathF.PI;
        _pitch = 0f;

        // FOV = 45°, небольшой запас по краям.
        _distance = MathF.Max(radius / MathF.Tan(MathF.PI / 8f) * 1.2f, 1f);
    }

    private (Vector3 Forward, Vector3 Right, Vector3 Up) GetCameraBasis()
    {
        var forward = new Vector3(
            MathF.Cos(_pitch) * MathF.Cos(_yaw),
            MathF.Sin(_pitch),
            MathF.Cos(_pitch) * MathF.Sin(_yaw));

        forward = Vector3.Normalize(forward);

        var right = Vector3.Cross(forward, Vector3.UnitY);
        if (right.LengthSquared() < 1e-6f)
            right = Vector3.UnitZ;
        else
            right = Vector3.Normalize(right);

        var up = Vector3.Normalize(Vector3.Cross(right, forward));

        return (forward, right, up);
    }

    #endregion

    #region private fields

    private CompositionCustomVisual? _visual;
    private Point _lastMousePos;

    private bool _cameraInitialized;

    private Vector3 _target = Vector3.Zero;

    // Стартовая камера: Position="10,0,0", LookDirection="-10,0,0"
    private float _yaw = MathF.PI;
    private float _pitch = 0f;
    private float _distance = 10f;

    private DragMode _dragMode;

    private const float RotateSensitivity = 0.01f;
    private const float PanSensitivity = 0.0025f;
    private const float ZoomSensitivity = 0.12f;
    private const float MaxPitch = 1.553343f; // ~89°

    private enum DragMode
    {
        None,
        Rotate,
        Pan
    }

    #endregion

    class GlVisual : CompositionCustomVisualHandler
    {
        public GlVisual(OpenGlContent content)
        {
            _content = content;
        }

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            var bounds = GetRenderBounds();
            var size = PixelSize.FromSize(bounds.Size, 1);
            if (size.Width < 1 || size.Height < 1 || _currentModel3DMessage is null)
                return;

            if (drawingContext.TryGetFeature<ISkiaSharpApiLeaseFeature>(out var skiaFeature))
            {
                using var skiaLease = skiaFeature.Lease();
                var grContext = skiaLease.GrContext;
                if (grContext == null)
                    return;

                SKImage? snapshot;
                using (var platformApiLease = skiaLease.TryLeasePlatformGraphicsApi())
                {
                    if (platformApiLease?.Context is not IGlContext glContext)
                        return;

                    var gl = glContext.GlInterface;
                    if (_glContext != glContext)
                    {
                        _fbo = null;
                        _contentInitialized = false;
                        _glContext = glContext;
                    }

                    gl.GetIntegerv(GL_FRAMEBUFFER_BINDING, out var oldFb);

                    _fbo ??= new OpenGlFbo(glContext, grContext);
                    if (_fbo.Size != size)
                        _fbo.Resize(size);

                    gl.BindFramebuffer(GL_FRAMEBUFFER, _fbo.Fbo);

                    if (!_contentInitialized)
                    {
                        _content.Init(gl, glContext.Version);
                        _contentInitialized = true;
                    }

                    _content.OnOpenGlRender(gl, _fbo.Fbo, size, _currentModel3DMessage);

                    snapshot = _fbo.Snapshot();
                    gl.BindFramebuffer(GL_FRAMEBUFFER, oldFb);
                }

                using (snapshot)
                    if (snapshot != null)
                        skiaLease.SkCanvas.DrawImage(snapshot, new SKRect(0, 0,
                            (float)bounds.Width, (float)bounds.Height));
            }
        }

        public override void OnAnimationFrameUpdate()
        {
            if (_reRender)
            {
                _reRender = false;
                Invalidate();
            }

            base.OnAnimationFrameUpdate();
        }

        public override void OnMessage(object message)
        {
            if (message is Model3DMessage model3DMessage)
            {
                _currentModel3DMessage = model3DMessage;
                _reRender = true;
                RegisterForNextAnimationFrameUpdate();
            }
            else if (message is DisposeMessage)
            {
                if (_glContext != null)
                {
                    try
                    {
                        if (_fbo != null || _contentInitialized)
                        {
                            using (_glContext.MakeCurrent())
                            {
                                if (_contentInitialized)
                                    _content.Deinit(_glContext.GlInterface);

                                _contentInitialized = false;
                                _fbo?.Dispose();
                                _fbo = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }

                    _glContext = null;
                }
            }

            base.OnMessage(message);
        }

        private readonly OpenGlContent _content;
        private Model3DMessage? _currentModel3DMessage;
        private bool _contentInitialized;
        private OpenGlFbo? _fbo;
        private bool _reRender;
        private IGlContext? _glContext;
    }

    public class DisposeMessage
    {
    }

    public class Model3DMessage
    {
        public Model3DScene? Model3DScene;
        public Vector3 Target;
        public float Yaw;
        public float Pitch;
        public float Distance;
    }
}