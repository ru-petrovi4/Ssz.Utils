using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Ssz.Operator.Core.DsShapeViews
{
    public abstract class ButtonDsShapeViewBase : ControlDsShapeView<Button>
    {
        #region construction and destruction

        protected ButtonDsShapeViewBase(ButtonDsShapeBase dsShape, ControlsPlay.Frame? frame)
            : base(new Button(), dsShape, frame)
        {
            Control.Focusable = false;

            if (!VisualDesignMode)
            {
                Control.PointerPressed += ButtonOnPointerPressed;
                Control.PointerReleased += ButtonOnPointerReleased;
                Control.Click += ButtonOnClick;
                Control.DoubleTapped += ButtonOnDoubleTapped;
                Control.PointerEntered += ButtonOnPointerEntered;
                Control.PointerExited += ButtonOnPointerExited;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
                if (!VisualDesignMode)
                {
                    if (_clickDsCommandView is not null) _clickDsCommandView.Dispose();
                    if (_doubleTappedDsCommandView is not null) _doubleTappedDsCommandView.Dispose();
                    if (_rightClickDsCommandView is not null) _rightClickDsCommandView.Dispose();
                    if (_holdDsCommandView is not null) _holdDsCommandView.Dispose();

                    if (_timer is not null) _timer.Dispose();
                }

            // Release unmanaged resources.
            // Set large fields to null.       
            _clickDsCommandView = null;
            _doubleTappedDsCommandView = null;
            _rightClickDsCommandView = null;
            _holdDsCommandView = null;
            _timer = null;

            base.Dispose(disposing);
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = DsShapeViewModel.DsShape as ButtonDsShapeBase;
            if (dsShape is null) return;

            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    Button.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container,
                    Button.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.ClickDsCommand))
                if (!VisualDesignMode)
                {
                    if (_clickDsCommandView is not null) _clickDsCommandView.Dispose();
                    _clickDsCommandView = new DsCommandView(Frame, dsShape.ClickDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.DoubleClickDsCommand))
                if (!VisualDesignMode)
                {
                    if (_doubleTappedDsCommandView is not null) _doubleTappedDsCommandView.Dispose();
                    _doubleTappedDsCommandView = new DsCommandView(Frame,
                        dsShape.DoubleClickDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.RightClickDsCommand))
                if (!VisualDesignMode)
                {
                    if (_rightClickDsCommandView is not null) _rightClickDsCommandView.Dispose();
                    _rightClickDsCommandView = new DsCommandView(Frame,
                        dsShape.RightClickDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.HoldDsCommand))
                if (!VisualDesignMode)
                {
                    if (_holdDsCommandView is not null) _holdDsCommandView.Dispose();
                    _holdDsCommandView = new DsCommandView(Frame, dsShape.HoldDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.PointerEnteredDsCommand))
                if (!VisualDesignMode)
                {
                    if (_pointerEnteredDsCommandView is not null) _pointerEnteredDsCommandView.Dispose();
                    _pointerEnteredDsCommandView = new DsCommandView(Frame, dsShape.PointerEnteredDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.PointerExitedDsCommand))
                if (!VisualDesignMode)
                {
                    if (_mouseExitedDsCommandView is not null) _mouseExitedDsCommandView.Dispose();
                    _mouseExitedDsCommandView = new DsCommandView(Frame, dsShape.PointerExitedDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.IsDefaultInfo))
                Control.SetBindingOrConst(dsShape.Container, Button.IsDefaultProperty, dsShape.IsDefaultInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);

            if (propertyName is null || propertyName == nameof(dsShape.IsCancelInfo))
                Control.SetBindingOrConst(dsShape.Container, Button.IsCancelProperty, dsShape.IsCancelInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        #endregion        

        #region private functions

        private void ButtonOnPointerPressed(object? sender, PointerPressedEventArgs args)
        {
            if (Disposed || !IsEnabled) return;

            if (_holdDsCommandView is not null && !_holdDsCommandView.IsEmpty)
            {
                if (_timer is not null) _timer.Dispose();
                _timer = new Timer(state => Dispatcher.UIThread.Invoke(TimerCallback), 
                    null,
                    ((ButtonDsShapeBase) DsShapeViewModel.DsShape).HoldCommandDelayMs,
                    ((ButtonDsShapeBase) DsShapeViewModel.DsShape).HoldCommandIntervalMs);
            }
        }

        private void ButtonOnClick(object? sender, RoutedEventArgs routedEventArgs)
        {
            if (Disposed || !IsEnabled) 
                return;

            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            _clickDsCommandView?.DoCommand();
        }

        private void ButtonOnDoubleTapped(object? sender, TappedEventArgs args)
        {
            if (Disposed || !IsEnabled) 
                return;

            _doubleTappedDsCommandView?.DoCommand();
        }

        private void ButtonOnPointerEntered(object? sender, PointerEventArgs args)
        {
            if (Disposed || !IsEnabled)
                return;

            _pointerEnteredDsCommandView?.DoCommand();
        }

        private void ButtonOnPointerExited(object? sender, PointerEventArgs args)
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            if (Disposed || !IsEnabled)
                return;

            _mouseExitedDsCommandView?.DoCommand();
        }

        private void ButtonOnPointerReleased(object? sender, PointerReleasedEventArgs args)
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            if (Disposed || !IsEnabled)
                return;

            if (args.InitialPressMouseButton == MouseButton.Right)
                _rightClickDsCommandView?.DoCommand();
        }

        private void TimerCallback()
        {
            if (Disposed) return;

            _holdDsCommandView?.DoCommand();
        }

        #endregion

        #region private fields

        private DsCommandView? _clickDsCommandView;
        private DsCommandView? _doubleTappedDsCommandView;
        private DsCommandView? _rightClickDsCommandView;
        private DsCommandView? _holdDsCommandView;
        private DsCommandView? _pointerEnteredDsCommandView;
        private DsCommandView? _mouseExitedDsCommandView;
        private Timer? _timer;

        #endregion
    }
}