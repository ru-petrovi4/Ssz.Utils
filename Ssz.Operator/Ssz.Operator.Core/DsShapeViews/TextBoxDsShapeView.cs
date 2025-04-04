using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class TextBoxDsShapeView : ControlDsShapeView<TextBox>, IAppliable
    {
        #region private fields

        private BindingExpressionBase? _textBindingExpression;

        #endregion

        #region public functions

        public void Apply()
        {
            if (Control.IsReadOnly) return;
            if (_textBindingExpression is null) return;

            _textBindingExpression.UpdateSource();
            ((ValueConverterBase) ((MultiBinding) _textBindingExpression.ParentBindingBase).Converter)
                .DisableUpdatingTarget = false;
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (TextBoxDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.TextInfo))
                _textBindingExpression = Control.SetBindingOrConst(dsShape.Container, TextBox.TextProperty,
                    dsShape.TextInfo,
                    BindingMode.TwoWay,
                    UpdateSourceTrigger.Explicit, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.TextAlignment))
                Control.TextAlignment = dsShape.TextAlignment;
            if (propertyName is null || propertyName == nameof(dsShape.TextWrapping))
                Control.TextWrapping = dsShape.TextWrapping;
            if (propertyName is null || propertyName == nameof(dsShape.IsReadOnlyInfo))
            {
                Control.SetBindingOrConst(dsShape.Container, TextBoxBase.IsReadOnlyProperty,
                    dsShape.IsReadOnlyInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);

                if (!VisualDesignMode && dsShape.IsReadOnlyInfo.IsConst && dsShape.IsReadOnlyInfo.ConstValue)
                    Control.Cursor = Cursors.Arrow;
                else
                    Control.Cursor = null;
            }
        }

        #endregion

        #region construction and destruction

        public TextBoxDsShapeView(TextBoxDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new TextBox(), dsShape, frame)
        {
            if (!VisualDesignMode)
            {
                Control.TextChanged += TextBoxOnTextChanged;
                Control.LostKeyboardFocus += TextBoxOnLostKeyboardFocus;
                Control.PreviewKeyDown += TextBoxOnPreviewKeyDown;
                Control.PreviewMouseUp += TextBoxOnPreviewMouseUp;
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                if (!VisualDesignMode)
                {
                    Control.TextChanged -= TextBoxOnTextChanged;
                    Control.LostKeyboardFocus -= TextBoxOnLostKeyboardFocus;
                    Control.PreviewKeyDown -= TextBoxOnPreviewKeyDown;
                    Control.PreviewMouseUp -= TextBoxOnPreviewMouseUp;
                }

            // Release unmanaged resources.
            // Set large fields to null.    
            _textBindingExpression = null;

            base.Dispose(disposing);
        }

        #endregion

        #region private functions

        private void TextBoxOnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (Control.IsReadOnly) return;
            if (_textBindingExpression is null ||
                ((ValueConverterBase) ((MultiBinding) _textBindingExpression.ParentBindingBase).Converter)
                .DisableUpdatingTarget) return;
            Control.SelectAll();
        }

        private void TextBoxOnLostKeyboardFocus(object? sender,
            KeyboardFocusChangedEventArgs keyboardFocusChangedEventArgs)
        {
            if (_textBindingExpression is null) return;
            ((ValueConverterBase) ((MultiBinding) _textBindingExpression.ParentBindingBase).Converter)
                .DisableUpdatingTarget = false;
        }

        private void TextBoxOnPreviewMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (Control.IsReadOnly) return;
            Application applicationCurrent = Application.Current;
            if (applicationCurrent is null) return;
            applicationCurrent.Dispatcher.BeginInvoke(new Action(() => Control.SelectAll()));
        }

        private void TextBoxOnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (Control.IsReadOnly) return;
            if (_textBindingExpression is null) return;
            switch (e.Key)
            {
                case Key.Enter:
                    _textBindingExpression.UpdateSource();
                    ((ValueConverterBase) ((MultiBinding) _textBindingExpression.ParentBindingBase).Converter)
                        .DisableUpdatingTarget = false;
                    Keyboard.ClearFocus();
                    break;
                case Key.Escape:
                    ((ValueConverterBase) ((MultiBinding) _textBindingExpression.ParentBindingBase).Converter)
                        .DisableUpdatingTarget = false;
                    Keyboard.ClearFocus();
                    break;
                default:
                    ((ValueConverterBase) ((MultiBinding) _textBindingExpression.ParentBindingBase).Converter)
                        .DisableUpdatingTarget = true;
                    break;
            }
        }

        #endregion
    }
}