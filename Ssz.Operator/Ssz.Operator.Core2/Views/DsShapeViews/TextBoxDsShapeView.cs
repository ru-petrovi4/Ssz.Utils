using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
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
            KeepOwnBrushesInEveryState();

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
            // HorizontalContentAlignment is deliberately not taken from the shape: it defaults to Center
            // and an EditBox hides it from the property grid, because a WPF TextBox ignored it and placed
            // its text by TextAlignment alone. Avalonia's TextBox does honour it, which would centre the
            // text of every EditBox. Stretch leaves TextAlignment in charge, as in WPF.
            Control.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
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

                if (!VisualDesignMode && dsShape.IsReadOnlyInfo.IsConst && dsShape.IsReadOnlyInfo.ConstValue)
                {
                    // A read-only WPF TextBox showed no caret and could not be typed into, so a shape
                    // like this was plain text on the drawing. Avalonia still focuses it, draws a caret
                    // and offers the text cursor, which makes it look editable.
                    Control.Focusable = false;
                    Control.Cursor = new Cursor(StandardCursorType.Arrow);
                }
                else
                {
                    Control.Focusable = true;
                    // Clearing the local value, not writing Cursor.Default: the theme setter puts the
                    // text cursor on an editable TextBox, and a local null would win over it.
                    Control.ClearValue(InputElement.CursorProperty);
                }
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Avalonia's TextBox theme repaints the border element of its template in the pointerover and
        ///     focus states with brushes of its own, ignoring the ones the shape sets. A shape carrying the
        ///     drawing background and no border then flashes a white box under the pointer, and hides its
        ///     own light text once focused. WPF kept the brushes of the control in every state, so they are
        ///     pinned back to it here.
        /// </summary>
        private void KeepOwnBrushesInEveryState()
        {
            foreach (string pseudoClass in new[] { ":pointerover", ":focus", ":focus-within" })
            {
                // The border element is named PART_BorderElement by the Fluent theme and border by the
                // Classic one; both themes are loaded, so both names are covered.
                foreach (string borderName in new[] { "PART_BorderElement", "border" })
                {
                    string name = borderName;
                    string cls = pseudoClass;
                    Styles.Add(new Style(x => x.OfType<TextBox>().Class(cls).Template()
                        .OfType<Border>().Name(name))
                    {
                        Setters =
                        {
                            new Setter(Border.BackgroundProperty,
                                new TemplateBinding(Avalonia.Controls.Primitives.TemplatedControl.BackgroundProperty)),
                            new Setter(Border.BorderBrushProperty,
                                new TemplateBinding(Avalonia.Controls.Primitives.TemplatedControl.BorderBrushProperty)),
                            new Setter(Border.BorderThicknessProperty,
                                new TemplateBinding(Avalonia.Controls.Primitives.TemplatedControl.BorderThicknessProperty))
                        }
                    });
                }
            }
        }

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
                    TopLevel.GetTopLevel(this)?.Focus();
                    break;
                case Key.Escape:
                    ((ValueConverterBase)_textBindingExpression.Item2.Converter!)
                        .DisableUpdatingTarget = false;
                    TopLevel.GetTopLevel(this)?.Focus();
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