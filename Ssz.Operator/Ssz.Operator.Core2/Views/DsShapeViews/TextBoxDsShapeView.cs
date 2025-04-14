using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class TextBoxDsShapeView : ControlDsShapeView<TextBox>, IAppliable
    {   
        #region construction and destruction

        public TextBoxDsShapeView(TextBoxDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new TextBox(), dsShape, frame)
        {
            if (!VisualDesignMode)
            {
                Control.TextChanged += TextBox_OnTextChanged;
                Control.LostFocus += TextBox_OnLostFocus;
                Control.KeyDown += TextBox_OnKeyDown;
                Control.PointerReleased += TextBox_OnPointerReleased;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                if (!VisualDesignMode)
                {
                    Control.TextChanged -= TextBox_OnTextChanged;
                    Control.LostFocus -= TextBox_OnLostFocus;
                    Control.KeyDown -= TextBox_OnKeyDown;
                    Control.PointerReleased -= TextBox_OnPointerReleased;
                }

            // Release unmanaged resources.
            // Set large fields to null.    
            _textBindingExpression = default;

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public void Apply()
        {
            if (Control.IsReadOnly) 
                return;
            if (_textBindingExpression.Item1 is null)
                return;

            var valueConverter = (ValueConverterBase)_textBindingExpression.Item2!.Converter!;
            valueConverter.ConvertBack(Control.Text, DsShapeViewModel, null, CultureInfo.InvariantCulture);            
            Dispatcher.UIThread.InvokeAsync(new Action(() => { valueConverter.DisableUpdatingTarget = false; }));
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (TextBoxDsShape)DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    TextBox.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container,
                    TextBox.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
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
                Control.SetBindingOrConst(dsShape.Container, TextBox.IsReadOnlyProperty,
                    dsShape.IsReadOnlyInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);

                //if (!VisualDesignMode && dsShape.IsReadOnlyInfo.IsConst && dsShape.IsReadOnlyInfo.ConstValue)
                //    Control.Cursor = Cursors.Arrow;
                //else
                //    Control.Cursor = null;
            }
        }

        #endregion

        #region private functions

        private void TextBox_OnTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (Control.IsReadOnly) 
                return;
            if (_textBindingExpression.Item2 is null ||
                ((ValueConverterBase)_textBindingExpression.Item2.Converter!)
                        .DisableUpdatingTarget)
                    return;
            Control.SelectAll();
        }

        private void TextBox_OnLostFocus(object? sender,
            RoutedEventArgs args)
        {
            if (_textBindingExpression.Item2 is null) 
                return;
            ((ValueConverterBase)_textBindingExpression.Item2.Converter!)
                .DisableUpdatingTarget = false;
        }

        private void TextBox_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (Control.IsReadOnly) 
                return;
            Dispatcher.UIThread.InvokeAsync(new Action(() => Control.SelectAll()));
        }

        private void TextBox_OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (Control.IsReadOnly) 
                return;
            if (_textBindingExpression.Item2 is null) 
                return;
            switch (e.Key)
            {
                case Key.Enter:
                    var valueConverter = (ValueConverterBase)_textBindingExpression.Item2!.Converter!;
                    valueConverter.ConvertBack(Control.Text, DsShapeViewModel, null, CultureInfo.InvariantCulture);
                    Dispatcher.UIThread.InvokeAsync(new Action(() => { valueConverter.DisableUpdatingTarget = false; }));                    
                    TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
                    break;
                case Key.Escape:
                    ((ValueConverterBase)_textBindingExpression.Item2.Converter!)
                        .DisableUpdatingTarget = false;
                    TopLevel.GetTopLevel(this)?.FocusManager?.ClearFocus();
                    break;
                default:
                    ((ValueConverterBase)_textBindingExpression.Item2.Converter!)
                        .DisableUpdatingTarget = true;
                    break;
            }
        }

        #endregion

        #region private fields

        private (BindingExpressionBase?, MultiBinding?) _textBindingExpression;

        #endregion
    }
}