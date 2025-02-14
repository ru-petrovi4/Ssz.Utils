using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public abstract class ButtonDsShapeViewBase : ControlDsShapeView<Button>
    {
        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = DsShapeViewModel.DsShape as ButtonDsShapeBase;
            if (dsShape is null) return;

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
                    if (_doubleClickDsCommandView is not null) _doubleClickDsCommandView.Dispose();
                    _doubleClickDsCommandView = new DsCommandView(Frame,
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

            if (propertyName is null || propertyName == nameof(dsShape.MouseEnterDsCommand))
                if (!VisualDesignMode)
                {
                    if (_mouseEnterDsCommandView is not null) _mouseEnterDsCommandView.Dispose();
                    _mouseEnterDsCommandView = new DsCommandView(Frame, dsShape.MouseEnterDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.MouseLeaveDsCommand))
                if (!VisualDesignMode)
                {
                    if (_mouseLeaveDsCommandView is not null) _mouseLeaveDsCommandView.Dispose();
                    _mouseLeaveDsCommandView = new DsCommandView(Frame, dsShape.MouseLeaveDsCommand,
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

        #region construction and destruction

        protected ButtonDsShapeViewBase(ButtonDsShapeBase dsShape, ControlsPlay.Frame? frame)
            : base(new Button(), dsShape, frame)
        {
            Control.Focusable = false;

            if (!VisualDesignMode)
            {
                Control.PreviewMouseLeftButtonDown += ButtonOnMouseLeftButtonDown;
                Control.PreviewMouseLeftButtonUp += ButtonOnMouseLeftButtonUp;
                Control.PreviewMouseRightButtonUp += ButtonOnMouseRightButtonUp;
                Control.Click += ButtonOnClick;
                Control.PreviewMouseDoubleClick += ButtonOnMouseDoubleClick;
                Control.MouseEnter += ButtonOnMouseEnter;
                Control.MouseLeave += ButtonOnMouseLeave;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
                if (!VisualDesignMode)
                {
                    if (_clickDsCommandView is not null) _clickDsCommandView.Dispose();
                    if (_doubleClickDsCommandView is not null) _doubleClickDsCommandView.Dispose();
                    if (_rightClickDsCommandView is not null) _rightClickDsCommandView.Dispose();
                    if (_holdDsCommandView is not null) _holdDsCommandView.Dispose();

                    if (_timer is not null) _timer.Dispose();
                }

            // Release unmanaged resources.
            // Set large fields to null.       
            _clickDsCommandView = null;
            _doubleClickDsCommandView = null;
            _rightClickDsCommandView = null;
            _holdDsCommandView = null;
            _timer = null;

            base.Dispose(disposing);
        }

        #endregion

        #region private functions

        private void ButtonOnMouseLeftButtonDown(object? sender, MouseButtonEventArgs args)
        {
            if (Disposed || !IsEnabled) return;

            if (_holdDsCommandView is not null && !_holdDsCommandView.IsEmpty)
            {
                if (_timer is not null) _timer.Dispose();
                _timer = new Timer(state => Dispatcher.Invoke(TimerCallback), null,
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

            if (_clickDsCommandView is not null) 
                _clickDsCommandView.DoCommand();
        }

        private void ButtonOnMouseDoubleClick(object? sender, MouseButtonEventArgs args)
        {
            if (Disposed || !IsEnabled || args.ChangedButton != MouseButton.Left) 
                return;

            if (_doubleClickDsCommandView is not null)
                _doubleClickDsCommandView.DoCommand();
        }

        private void ButtonOnMouseEnter(object? sender, MouseEventArgs args)
        {
            if (Disposed || !IsEnabled)
                return;

            if (_mouseEnterDsCommandView is not null)
                _mouseEnterDsCommandView.DoCommand();
        }

        private void ButtonOnMouseLeave(object? sender, MouseEventArgs args)
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }

            if (Disposed || !IsEnabled)
                return;

            if (_mouseLeaveDsCommandView is not null)
                _mouseLeaveDsCommandView.DoCommand();
        }

        private void ButtonOnMouseLeftButtonUp(object? sender, MouseButtonEventArgs args)
        {
            if (_timer is not null)
            {
                _timer.Dispose();
                _timer = null;
            }
        }

        private void ButtonOnMouseRightButtonUp(object? sender, MouseButtonEventArgs args)
        {
            if (Disposed || !IsEnabled)
                return;

            if (_rightClickDsCommandView is not null)
                _rightClickDsCommandView.DoCommand();
        }

        private void TimerCallback()
        {
            if (Disposed) return;

            if (_holdDsCommandView is not null) _holdDsCommandView.DoCommand();
        }

        #endregion

        #region private fields

        private DsCommandView? _clickDsCommandView;
        private DsCommandView? _doubleClickDsCommandView;
        private DsCommandView? _rightClickDsCommandView;
        private DsCommandView? _holdDsCommandView;
        private DsCommandView? _mouseEnterDsCommandView;
        private DsCommandView? _mouseLeaveDsCommandView;
        private Timer? _timer;

        #endregion
    }
}