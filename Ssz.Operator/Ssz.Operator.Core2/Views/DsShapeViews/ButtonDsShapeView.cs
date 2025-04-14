using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ButtonDsShapeView : ButtonDsShapeViewBase
    {
        #region construction and destruction

        public ButtonDsShapeView(ButtonDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            _contentContainerControl = new ContentControl();
            _textContainerControl = new ContentControl();
            _textBlock = new TextBlock();

            var grid = new Grid();
            grid.Children.Add(_contentContainerControl);
            grid.Children.Add(_textContainerControl);
            Control.Content = grid;
        }

        #endregion

        #region public functions

        public static readonly AvaloniaProperty UnPressedContentProperty = AvaloniaProperty.Register<ButtonDsShapeView, object?>(
            "UnPressedContent",
            null);

        public static readonly AvaloniaProperty PressedContentProperty = AvaloniaProperty.Register<ButtonDsShapeView, object?>(
            "PressedContent",
            null);

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ButtonDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.ContentHorizontalAlignment))
                _contentContainerControl.SetConst(dsShape.Container, HorizontalAlignmentProperty,
                    dsShape.ContentHorizontalAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.ContentVerticalAlignment))
                _contentContainerControl.SetConst(dsShape.Container, VerticalAlignmentProperty,
                    dsShape.ContentVerticalAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.ContentInfo)
                                     || propertyName == nameof(dsShape.PressedContentInfo))
            {
                if (VisualDesignMode ||
                    dsShape.PressedContentInfo.IsConst && dsShape.PressedContentInfo.ConstValue.IsEmpty)
                {
                    ClearValue(UnPressedContentProperty);

                    ClearValue(PressedContentProperty);

                    _contentContainerControl.SetBindingOrConst(dsShape.Container, ContentControl.ContentProperty,
                        dsShape.ContentInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default, VisualDesignMode);
                }
                else
                {
                    this.SetBindingOrConst(dsShape.Container, UnPressedContentProperty, dsShape.ContentInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default, VisualDesignMode);

                    this.SetBindingOrConst(dsShape.Container, PressedContentProperty, dsShape.PressedContentInfo,
                        BindingMode.OneWay,
                        UpdateSourceTrigger.Default, VisualDesignMode);

                    var multiBinding = new MultiBinding();
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "UnPressedContent",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = this,
                        Path = "PressedContent",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = Control,
                        Path = "IsPressed",
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Converter = new ContentConverter();
                    _contentContainerControl.Bind(ContentControl.ContentProperty,
                        multiBinding);
                }
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextMargin))
                _textContainerControl.SetConst(dsShape.Container, MarginProperty, dsShape.TextMargin);
            if (propertyName is null || propertyName == nameof(dsShape.TextStretch))
            {
                if (dsShape.TextStretch == Stretch.None)
                {
                    var viewBox = _textContainerControl.Content as Viewbox;
                    if (viewBox is not null) viewBox.Child = null;
                    _textContainerControl.Content = _textBlock;
                }
                else
                {
                    var viewBox = _textContainerControl.Content as Viewbox;
                    if (viewBox is null)
                    {
                        viewBox = new Viewbox();
                        _textContainerControl.Content = viewBox;
                        viewBox.Child = _textBlock;
                    }

                    viewBox.Stretch = dsShape.TextStretch;
                }
            }
            
            if (propertyName is null || propertyName == nameof(dsShape.ForegroundInfo))
                _textBlock.SetBindingOrConst(dsShape.Container, TextBlock.ForegroundProperty,
                    dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.TextInfo))
                _textBlock.SetBindingOrConst(dsShape.Container, TextBlock.TextProperty, dsShape.TextInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.DsFont))
            {
                FontFamily? fontFamily;
                double fontSize;
                FontStyle? fontStyle;
                FontStretch? fontStretch;
                FontWeight? fontWeight;
                ConstantsHelper.ComputeFont(dsShape.Container, dsShape.DsFont,
                    out fontFamily, out fontSize, out fontStyle, out fontStretch, out fontWeight);

                _textBlock.SetConst(dsShape.Container, TextBlock.FontFamilyProperty, fontFamily);
                if (fontSize > 0.0) _textBlock.SetConst(dsShape.Container, TextBlock.FontSizeProperty, fontSize);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontStyleProperty, fontStyle);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontStretchProperty, fontStretch);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontWeightProperty, fontWeight);
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextHorizontalAlignment))
            {
                switch (dsShape.TextHorizontalAlignment)
                {
                    case TextAlignment.Left:
                        _textContainerControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                        break;
                    case TextAlignment.Center:
                        _textContainerControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                        break;
                    case TextAlignment.Right:
                        _textContainerControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                        break;
                    case TextAlignment.Justify:
                        _textContainerControl.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                        break;
                }

                _textBlock.SetConst(dsShape.Container, TextBlock.TextAlignmentProperty,
                    dsShape.TextHorizontalAlignment);
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextVerticalAlignment))
                _textContainerControl.SetConst(dsShape.Container, VerticalAlignmentProperty,
                    dsShape.TextVerticalAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.TextWrapping))
                _textBlock.SetConst(dsShape.Container, TextBlock.TextWrappingProperty,
                    dsShape.TextWrapping);            
        }

        #endregion

        #region private fields

        private readonly ContentControl _contentContainerControl;
        private readonly ContentControl _textContainerControl;
        private readonly TextBlock _textBlock;

        #endregion

        private class ContentConverter : IMultiValueConverter
        {
            #region public functions

            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values is null || values.Count != 3) return BindingOperations.DoNothing;
                var v2 = values[2];
                if (v2 is null) return BindingOperations.DoNothing;
                if ((bool)v2) return values[1];
                return values[0];
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
                CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}