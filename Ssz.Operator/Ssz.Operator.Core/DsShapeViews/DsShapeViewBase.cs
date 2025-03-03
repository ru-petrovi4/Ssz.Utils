using System;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Ssz.Operator.Core.ControlsCommon.Converters;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public abstract partial class DsShapeViewBase : FrameworkElement, IDisposable
    {
        #region construction and destruction

        protected DsShapeViewBase(DsShapeBase dsShape, ControlsPlay.Frame? frame)
        {
            Frame = frame;

            DsShapeViewModel = new DsShapeViewModel(dsShape, frame is not null ? frame.PlayWindow : null);
            DataContext = DsShapeViewModel;

            if (VisualDesignMode) DsShapeViewModel.DsShapeChanged += OnDsShapeChanged;
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

            _content = null;
            Frame = null;

            Disposed = true;
        }

        ~DsShapeViewBase()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty CenterDeltaPositionXProperty =
            DependencyProperty.Register("CenterDeltaPositionX", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty CenterDeltaPositionYProperty =
            DependencyProperty.Register("CenterDeltaPositionY", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty WidthDeltaProperty =
            DependencyProperty.Register("WidthDelta", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty HeightDeltaProperty =
            DependencyProperty.Register("HeightDelta", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty AngleDeltaProperty =
            DependencyProperty.Register("AngleDelta", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(0.0));

        public static readonly DependencyProperty ScaleXProperty =
            DependencyProperty.Register("ScaleX", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(1.0));

        public static readonly DependencyProperty ScaleYProperty =
            DependencyProperty.Register("ScaleY", typeof(double), typeof(DsShapeViewBase),
                new PropertyMetadata(1.0));

        public static PropertyPath GetPropertyPath<T>(Expression<Func<T>> propertyNameExpression)
        {
            return new(((MemberExpression)propertyNameExpression.Body).Member.Name);
        }

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
                    var content = Content as UIElement;
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
                    Content = (FrameworkElement)content;
                }
            }
        }

        public DsShapeViewModel DsShapeViewModel { get; }

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
                    Visibility = Visibility.Visible;
                }
                else
                {
                    dsShape.IsVisibleInfo.FallbackValue = Visibility.Hidden;
                    this.SetVisibilityBindingOrConst(dsShape.Container, VisibilityProperty, dsShape.IsVisibleInfo,
                        Visibility.Hidden, VisualDesignMode);
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
                SetBinding(WidthProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.WidthInitial),
                    Mode = BindingMode.OneWay
                });

                SetBinding(HeightProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.HeightInitial),
                    Mode = BindingMode.OneWay
                });

                BindingOperations.SetBinding(this, Canvas.LeftProperty, new Binding
                {
                    Path =
                        GetPropertyPath(() => dsShapeViewModel.LeftNotTransformed),
                    Mode = BindingMode.OneWay
                });

                BindingOperations.SetBinding(this, Canvas.TopProperty, new Binding
                {
                    Path =
                        GetPropertyPath(() => dsShapeViewModel.TopNotTransformed),
                    Mode = BindingMode.OneWay
                });

                BindingOperations.SetBinding(this, RenderTransformOriginProperty, new Binding
                {
                    Path =
                        GetPropertyPath(() => dsShapeViewModel.CenterRelativePosition),
                    Mode =
                        BindingMode
                            .OneWay
                });

                RotateTransform = new RotateTransform();
                BindingOperations.SetBinding(RotateTransform, RotateTransform.AngleProperty,
                    new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.AngleInitial),
                        Mode = BindingMode.OneWay
                    });

                ScaleTranform = new ScaleTransform();
                BindingOperations.SetBinding(ScaleTranform, ScaleTransform.ScaleXProperty,
                    new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.IsFlipped),
                        Converter = FlipBoolConverter.Instance,
                        Mode = BindingMode.OneWay
                    });

                BindingOperations.SetBinding(this, Panel.ZIndexProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.ZIndex),
                    Mode = BindingMode.OneWay
                });

                SetBinding(RotationXProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.RotationX),
                    Mode = BindingMode.OneWay
                });

                SetBinding(RotationYProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.RotationY),
                    Mode = BindingMode.OneWay
                });

                SetBinding(RotationZProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.RotationZ),
                    Mode = BindingMode.OneWay
                });

                SetBinding(FieldOfViewProperty, new Binding
                {
                    Path = GetPropertyPath(() => dsShapeViewModel.FieldOfView),
                    Mode = BindingMode.OneWay
                });
            }
            else
            {
                if (dsShape.WidthDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        SetBinding(WidthProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.WidthInitial),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Width = dsShape.WidthInitialNotRounded;
                }
                else
                {
                    if (VisualDesignMode)
                        SetBinding(WidthDeltaProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.WidthDelta),
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
                        Path = GetPropertyPath(() => dsShapeViewModel.WidthInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("WidthDelta"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.WidthFinal),
                        Mode = BindingMode.OneWay
                    });
                    SetBinding(WidthProperty, multiBinding);
                }

                if (dsShape.HeightDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        SetBinding(HeightProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.HeightInitial),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Height = dsShape.HeightInitialNotRounded;
                }
                else
                {
                    if (VisualDesignMode)
                        SetBinding(HeightDeltaProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.HeightDelta),
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
                        Path = GetPropertyPath(() => dsShapeViewModel.HeightInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("HeightDelta"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.HeightFinal),
                        Mode = BindingMode.OneWay
                    });
                    SetBinding(HeightProperty, multiBinding);
                }

                if (dsShape.CenterDeltaPositionXInfo.IsConst && dsShape.WidthDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        BindingOperations.SetBinding(this, Canvas.LeftProperty, new Binding
                        {
                            Path =
                                GetPropertyPath(() => dsShapeViewModel.LeftNotTransformed),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Canvas.SetLeft(this, dsShape.LeftNotTransformed);
                }
                else
                {
                    if (VisualDesignMode)
                        SetBinding(CenterDeltaPositionXProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.CenterDeltaPositionX),
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
                        Path = GetPropertyPath(() => dsShapeViewModel.CenterInitialPositionX),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("CenterDeltaPositionX"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.CenterFinalPositionX),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = new PropertyPath("CenterRelativePosition.X"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.WidthInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("WidthDelta"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.WidthFinal),
                        Mode = BindingMode.OneWay
                    });
                    BindingOperations.SetBinding(this, Canvas.LeftProperty, multiBinding);
                }

                if (dsShape.CenterDeltaPositionYInfo.IsConst &&
                    dsShape.HeightDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                        BindingOperations.SetBinding(this, Canvas.TopProperty, new Binding
                        {
                            Path =
                                GetPropertyPath(() => dsShapeViewModel.TopNotTransformed),
                            Mode = BindingMode.OneWay
                        });
                    else
                        Canvas.SetTop(this, dsShape.TopNotTransformed);
                }
                else
                {
                    if (VisualDesignMode)
                        SetBinding(CenterDeltaPositionYProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.CenterDeltaPositionY),
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
                        Path = GetPropertyPath(() => dsShapeViewModel.CenterInitialPositionY),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("CenterDeltaPositionY"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.CenterFinalPositionY),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = new PropertyPath("CenterRelativePosition.Y"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.HeightInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("HeightDelta"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.HeightFinal),
                        Mode = BindingMode.OneWay
                    });
                    BindingOperations.SetBinding(this, Canvas.TopProperty, multiBinding);
                }

                if (dsShape.AngleDeltaInfo.IsConst)
                {
                    if (VisualDesignMode)
                    {
                        RotateTransform = new RotateTransform();
                        BindingOperations.SetBinding(RotateTransform, RotateTransform.AngleProperty,
                            new Binding
                            {
                                Path = GetPropertyPath(() => dsShapeViewModel.AngleInitial),
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
                        SetBinding(AngleDeltaProperty, new Binding
                        {
                            Path = GetPropertyPath(() => dsShapeViewModel.AngleDelta),
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
                        Path = GetPropertyPath(() => dsShapeViewModel.AngleInitial),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = new PropertyPath("AngleDelta"),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Path = GetPropertyPath(() => dsShapeViewModel.AngleFinal),
                        Mode = BindingMode.OneWay
                    });
                    RotateTransform = new RotateTransform();
                    BindingOperations.SetBinding(RotateTransform, RotateTransform.AngleProperty, multiBinding);
                }

                RenderTransformOrigin = dsShape.CenterRelativePosition;

                if (dsShape.IsFlipped)
                {
                    ScaleTranform = new ScaleTransform();
                    ScaleTranform.ScaleX = -1;
                }

                Panel.SetZIndex(this, dsShapeViewModel.ZIndex);

                RotationX = dsShapeViewModel.RotationX;
                RotationY = dsShapeViewModel.RotationY;
                RotationZ = dsShapeViewModel.RotationZ;
                FieldOfView = dsShapeViewModel.FieldOfView;
            }

            var transformGroup = new TransformGroup();
            if (ScaleTranform is not null) transformGroup.Children.Add(ScaleTranform);
            if (RotateTransform is not null) transformGroup.Children.Add(RotateTransform);
            if (transformGroup.Children.Count > 0)
                RenderTransform = transformGroup;
        }

        #endregion        

        #region private fields

        private FrameworkElement? _content;
        private bool _isHighlighted;

        #endregion
    }
}