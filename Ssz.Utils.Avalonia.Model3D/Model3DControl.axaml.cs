using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
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
            _visual.Size = new Vector(size.Width, size.Height);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _lastMousePos = e.GetPosition(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var currentPos = e.GetPosition(this);
            var delta = currentPos - _lastMousePos;

            _rotationY += (float)delta.X * 0.005f;
            _rotationX += (float)delta.Y * 0.005f;

            _lastMousePos = currentPos;
            _visual?.SendHandlerMessage(new Model3D
            {
                Model3DScene = null,
                RotationX = _rotationX,
                RotationY = _rotationY,
                Zoom = _zoom
            });
        }
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _zoom -= (float)e.Delta.Y * 0.5f;
        _zoom = Math.Max(1.0f, Math.Min(10.0f, _zoom));
        _visual?.SendHandlerMessage(new Model3D
        {
            Model3DScene = null,
            RotationX = _rotationX,
            RotationY = _rotationY,
            Zoom = _zoom
        });
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == DataProperty)
            OnDataPropertyChanged();
    }

    private void OnDataPropertyChanged()
    {
        _visual?.SendHandlerMessage(new Model3D
        {
            Model3DScene = Data,
            RotationX = _rotationX,
            RotationY = _rotationY,
            Zoom = _zoom
        });
    }

    #endregion

    #region private fields

    private CompositionCustomVisual? _visual;        
    private float _rotationX, _rotationY;
    private float _zoom = 5.0f;
    private Point _lastMousePos;

    #endregion

    class GlVisual : CompositionCustomVisualHandler
    {
        #region construction and destruction

        public GlVisual(OpenGlContent content)
        {
            _content = content;
        }

        #endregion

        #region public functions

        public override void OnRender(ImmediateDrawingContext drawingContext)
        {
            var bounds = GetRenderBounds();
            var size = PixelSize.FromSize(bounds.Size, 1);
            if (size.Width < 1 || size.Height < 1 || _model3D is null)
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
                        // The old context is lost
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

                    _content.OnOpenGlRender(gl, _fbo.Fbo, size, _model3D);

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
            if (message is Model3D model3DMessage)
            {
                _model3D = model3DMessage;
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
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    _glContext = null;
                }
            }

            base.OnMessage(message);
        }

        #endregion        

        #region private fields

        private OpenGlContent _content;
        private Model3D? _model3D;
        private bool _contentInitialized;
        private OpenGlFbo? _fbo;
        private bool _reRender;
        private IGlContext? _glContext;

        #endregion
    }

    public class DisposeMessage
    {

    }    
}