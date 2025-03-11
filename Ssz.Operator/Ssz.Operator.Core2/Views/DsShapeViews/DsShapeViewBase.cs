using System;
using System.Linq.Expressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Ssz.Operator.Core.ControlsCommon.Converters;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public abstract partial class DsShapeViewBase : ContentControl, IDisposable
    {
        #region construction and destruction

        protected DsShapeViewBase(DsShapeBase dsShape, ControlsPlay.Frame? frame)
        {
            Frame = frame;

            DataContext = new DsShapeViewModel(dsShape, frame is not null ? frame.PlayWindow : null);            

            if (VisualDesignMode) 
                DsShapeViewModel.DsShapeChanged += OnDsShapeChanged;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                // You shuld not set content to null, since this method can be invoked from NOT UI thread.
                // Clears all events subscriptions.
                DsShapeViewModel.Dispose();
            }

            Frame = null;

            Disposed = true;
        }

        ~DsShapeViewBase()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly AvaloniaProperty CenterDeltaPositionXProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("CenterDeltaPositionX", 0.0);

        public static readonly AvaloniaProperty CenterDeltaPositionYProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("CenterDeltaPositionY", 0.0);

        public static readonly AvaloniaProperty WidthDeltaProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("WidthDelta", 0.0);

        public static readonly AvaloniaProperty HeightDeltaProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("HeightDelta", 0.0);

        public static readonly AvaloniaProperty AngleDeltaProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("AngleDelta", 0.0);

        public static readonly AvaloniaProperty ScaleXProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("ScaleX", 1.0);

        public static readonly AvaloniaProperty ScaleYProperty =
            AvaloniaProperty.Register<DsShapeViewBase, double>("ScaleY", 1.0);        

        public bool Disposed { get; private set; }

        public RotateTransform? RotateTransform { get; private set; }

        public ScaleTransform? ScaleTranform { get; private set; }

        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (value == _isHighlighted) return;
                _isHighlighted = value;
                if (_isHighlighted)
                {
                    var content = Content as Control;
                    if (content is null) return;
                    Content = null;
                    var grid = new Grid();
                    grid.Children.Add(content);
                    grid.Children.Add(
                        new Border
                        {
                            BorderThickness = new Thickness(5),
                            BorderBrush = BlinkingDsBrush.GetBrush(Colors.Transparent, Colors.Yellow)
                        });
                    Content = grid;
                }
                else
                {
                    var grid = Content as Grid;
                    if (grid is null) return;
                    var content = grid.Children[0];
                    grid.Children.Clear();
                    Content = (Control)content;
                }
            }
        }

        public DsShapeViewModel DsShapeViewModel => (DsShapeViewModel)DataContext!;

        public ControlsPlay.Frame? Frame { get; private set; }

        public bool VisualDesignMode => Frame is null;

        public virtual void Initialize(PlayDrawingViewModel? playDrawingViewModel)
        {
            DsShapeViewModel.Close();
            DsShapeViewModel.Initialize(playDrawingViewModel);

            SetBaseBindings();

            OnDsShapeChanged(null);            
        }

        #endregion

        #region protected functions

        protected virtual void OnDsShapeChanged(string? propertyName)
        {
            DsShapeBase dsShape = DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.IsVisibleInfo))
            {
                if (VisualDesignMode && dsShape.GetParentComplexDsShape() is null)
                {
                    IsVisible = true;                    
                }
                else
                {
                    dsShape.IsVisibleInfo.FallbackValue = false;
                    this.SetVisibilityBindingOrConst(dsShape.Container, IsVisibleProperty, dsShape.IsVisibleInfo,
                        false, VisualDesignMode);
                }
            }

            if (propertyName is null || propertyName == nameof(dsShape.IsEnabledInfo))
                this.SetBindingOrConst(dsShape.Container, IsEnabledProperty, dsShape.IsEnabledInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.OpacityInfo))
                this.SetBindingOrConst(dsShape.Container, OpacityProperty, dsShape.OpacityInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        #endregion

        #region private functions

        private void SetBaseBindings()
        {
            DsShapeViewModel dsShapeViewModel = DsShapeViewModel;
            DsShapeBase dsShape = dsShapeViewModel.DsShape;

            if (VisualDesignMode && dsShape.GetParentComplexDsShape() is null)
            {
                Bind(WidthProperty, new Binding
                {
                    Path = nameof (DsShapeViewModel.WidthInitial),
                    Mode = BindingMode.OneWay
                });

                Bind(HeightProperty, new Binding
                {
                    Path = nameof (DsShapeViewModel.HeightInitial),
                    Mode = BindingMode.OneWay
                });

                Bind(Canvas.LeftProperty, new Binding
                {
                    Path =
                        nameof (DsShapeViewModel.LeftNotTransformed),
                    Mode = BindingMode.OneWay
                });

                Bind(Canvas.TopProperty, new Binding
                {
                    Path =
                        nameof (DsShapeViewModel.TopNotTransformed),
                    Mode = BindingMode.OneWay
                });

                Bind(RenderTransformOriginProperty, new Binding
                {
                    Path =
                        nameof (DsShapeViewModel.CenterRelativePosition),
                    Mode =
                        BindingMode
                            .OneWay
                });

                RotateTransform = new RotateTransform();
                RotateTransform.Bind(RotateTransform.AngleProperty,
                    new Binding
                    {
                        Source = DataContext,
                        Path = nameof (DsShapeViewModel.AngleInitial),
                        Mode = BindingMode.OneWay
                    });

                ScaleTranform = new ScaleTransform();
                ScaleTranform.Bind(ScaleTransform.ScaleXProperty,
                    new Binding
                    {
                        Path = nameof (DsShapeViewModel.IsFlipped),
                        Converter = FlipBoolConverter.Instance,
                        Mode = BindingMode.OneWay
                    });

                Bind(Canvas.ZIndexProperty, new Binding
                {
                    Path = nameof (DsShapeViewModel.ZIndex),
                    Mode = BindingMode.OneWay
                });

                //Bind(RotationXProperty, new Binding
                //{
                //    Path = nameof (DsShapeViewModel.RotationX),
                //    Mode = BindingMode.OneWay
                //});

                //Bind(RotationYProperty, new Binding
                //{
                //    Path = nameof (DsShapeViewModel.RotationY),
                //    Mode = BindingMode.OneWay
                //});

                //Bind(RotationZProperty, new Binding
                //{
                //    Path = nameof (DsShapeViewModel.RotationZ),
                //    Mode = BindingMode.OneWay
                //});

                //Bind(FieldOfViewProperty, new Binding
                //{
                //    Path = nameof (DsShapeViewModel.FieldOfView),
                //    Mode = BindingMode.OneWay
                //});
            }
            else
            {
                if (dsShape.WidthDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        Bind(WidthProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.WidthInitial),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Width = dsShape.WidthInitialNotRounded;
                }
                else
                {
                    if (VisualDesignMode)
                        Bind(WidthDeltaProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.WidthDelta),
                            Mode = BindingMode.OneWay
                        });
                    else
                        this.SetBindingOrConst(dsShape.Container, WidthDeltaProperty, dsShape.WidthDeltaInfo,
                            BindingMode.OneWay,
                            UpdateSourceTrigger.Default);

                    var multiBinding = new MultiBinding
                    {
                        Converter = InitialDeltaFinalConverter.Instance,
                        Mode = BindingMode.OneWay
                    };
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.WidthInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "WidthDelta",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.WidthFinal),
                        Mode = BindingMode.OneWay
                    });
                    Bind(WidthProperty, multiBinding);
                }

                if (dsShape.HeightDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        Bind(HeightProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.HeightInitial),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Height = dsShape.HeightInitialNotRounded;
                }
                else
                {
                    if (VisualDesignMode)
                        Bind(HeightDeltaProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.HeightDelta),
                            Mode = BindingMode.OneWay
                        });
                    else
                        this.SetBindingOrConst(dsShape.Container, HeightDeltaProperty, dsShape.HeightDeltaInfo,
                            BindingMode.OneWay,
                            UpdateSourceTrigger.Default);

                    var multiBinding = new MultiBinding
                    {
                        Converter = InitialDeltaFinalConverter.Instance,
                        Mode = BindingMode.OneWay
                    };
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.HeightInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "HeightDelta",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.HeightFinal),
                        Mode = BindingMode.OneWay
                    });
                    Bind(HeightProperty, multiBinding);
                }

                if (dsShape.CenterDeltaPositionXInfo.IsConst && dsShape.WidthDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        Bind(Canvas.LeftProperty, new Binding
                        {
                            Path =
                                nameof (DsShapeViewModel.LeftNotTransformed),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Canvas.SetLeft(this, dsShape.LeftNotTransformed);
                }
                else
                {
                    if (VisualDesignMode)
                        Bind(CenterDeltaPositionXProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.CenterDeltaPositionX),
                            Mode = BindingMode.OneWay
                        });
                    else
                        this.SetBindingOrConst(dsShape.Container, CenterDeltaPositionXProperty,
                            dsShape.CenterDeltaPositionXInfo,
                            BindingMode.OneWay,
                            UpdateSourceTrigger.Default);

                    var multiBinding = new MultiBinding
                    {
                        Converter = LeftTopConverter.Instance,
                        Mode = BindingMode.OneWay
                    };
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.CenterInitialPositionX),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "CenterDeltaPositionX",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.CenterFinalPositionX),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = "CenterRelativePosition.X",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.WidthInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "WidthDelta",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.WidthFinal),
                        Mode = BindingMode.OneWay
                    });
                    Bind(Canvas.LeftProperty, multiBinding);
                }

                if (dsShape.CenterDeltaPositionYInfo.IsConst &&
                    dsShape.HeightDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        Bind(Canvas.TopProperty, new Binding
                        {
                            Path =
                                nameof (DsShapeViewModel.TopNotTransformed),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Canvas.SetTop(this, dsShape.TopNotTransformed);
                }
                else
                {
                    if (VisualDesignMode)
                        Bind(CenterDeltaPositionYProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.CenterDeltaPositionY),
                            Mode = BindingMode.OneWay
                        });
                    else
                        this.SetBindingOrConst(dsShape.Container, CenterDeltaPositionYProperty,
                            dsShape.CenterDeltaPositionYInfo,
                            BindingMode.OneWay,
                            UpdateSourceTrigger.Default);

                    var multiBinding = new MultiBinding
                    {
                        Converter = LeftTopConverter.Instance,
                        Mode = BindingMode.OneWay
                    };
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.CenterInitialPositionY),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "CenterDeltaPositionY",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.CenterFinalPositionY),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = "CenterRelativePosition.Y",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.HeightInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "HeightDelta",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = nameof (DsShapeViewModel.HeightFinal),
                        Mode = BindingMode.OneWay
                    });
                    Bind(Canvas.TopProperty, multiBinding);
                }

                if (dsShape.AngleDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                    {
                        RotateTransform = new RotateTransform();
                        RotateTransform.Bind(RotateTransform.AngleProperty,
                            new Binding
                            {
                                Source = DataContext,
                                Path = nameof (DsShapeViewModel.AngleInitial),
                                Mode = BindingMode.OneWay
                            });
                    }
                    else
                    {
                        if (dsShape.AngleInitial != 0)
                        {
                            RotateTransform = new RotateTransform();
                            RotateTransform.Angle = dsShape.AngleInitialNotRounded;
                        }
                    }
                }
                else
                {
                    if (VisualDesignMode)
                        Bind(AngleDeltaProperty, new Binding
                        {
                            Path = nameof (DsShapeViewModel.AngleDelta),
                            Mode = BindingMode.OneWay
                        });
                    else
                        this.SetBindingOrConst(dsShape.Container, AngleDeltaProperty, dsShape.AngleDeltaInfo,
                            BindingMode.OneWay,
                            UpdateSourceTrigger.Default);

                    var multiBinding = new MultiBinding
                    {
                        Converter = InitialDeltaFinalConverter.Instance,
                        Mode = BindingMode.OneWay
                    };
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = DataContext,
                        Path = nameof (DsShapeViewModel.AngleInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "AngleDelta",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = DataContext,
                        Path = nameof (DsShapeViewModel.AngleFinal),
                        Mode = BindingMode.OneWay
                    });
                    RotateTransform = new RotateTransform();
                    RotateTransform.Bind(RotateTransform.AngleProperty, multiBinding);
                }

                RenderTransformOrigin = new RelativePoint(dsShape.CenterRelativePosition, RelativeUnit.Relative);

                if (dsShape.IsFlipped)
                {
                    ScaleTranform = new ScaleTransform();
                    ScaleTranform.ScaleX = -1;
                }

                SetValue(Canvas.ZIndexProperty, dsShapeViewModel.ZIndex);

                //RotationX = dsShapeViewModel.RotationX;
                //RotationY = dsShapeViewModel.RotationY;
                //RotationZ = dsShapeViewModel.RotationZ;
                //FieldOfView = dsShapeViewModel.FieldOfView;
            }

            var transformGroup = new TransformGroup();
            if (ScaleTranform is not null) transformGroup.Children.Add(ScaleTranform);
            if (RotateTransform is not null) transformGroup.Children.Add(RotateTransform);
            if (transformGroup.Children.Count > 0)
                RenderTransform = transformGroup;
        }

        #endregion        

        #region private fields
        
        private bool _isHighlighted;

        #endregion
    }
}