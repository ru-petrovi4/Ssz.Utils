using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Avalonia.Layout;
using Avalonia.Data;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class TextBlockDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public TextBlockDsShapeView(TextBlockDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            _textBlock = new TextBlock();

            IsHitTestVisible = false;
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (TextBlockDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.ForegroundInfo))
                _textBlock.SetBindingOrConst(dsShape.Container, TextBlock.ForegroundProperty,
                    dsShape.ForegroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.TextInfo))
                _textBlock.SetBindingOrConst(dsShape.Container, TextBlock.TextProperty, dsShape.TextInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.TextStretch))
            {
                if (dsShape.TextStretch == Stretch.None)
                {
                    var viewBox = Content as Viewbox;
                    if (viewBox is not null) viewBox.Child = null;
                    Content = _textBlock;
                }
                else
                {
                    var viewBox = Content as Viewbox;
                    if (viewBox is null)
                    {
                        viewBox = new Viewbox();
                        SetContentHorizontalAlignment(viewBox, dsShape.TextAlignment);
                        viewBox.VerticalAlignment = dsShape.VerticalAlignment;
                        Content = viewBox;
                        viewBox.Child = _textBlock;
                    }

                    viewBox.Stretch = dsShape.TextStretch;
                }
            }

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

            if (propertyName is null || propertyName == nameof(dsShape.TextDecorations))
                _textBlock.SetConst(dsShape.Container, TextBlock.TextDecorationsProperty, dsShape.TextDecorations);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalAlignment))
            {
                _textBlock.SetConst(dsShape.Container, VerticalAlignmentProperty, dsShape.VerticalAlignment);
                var viewBox = Content as Viewbox;
                if (viewBox is not null) viewBox.VerticalAlignment = dsShape.VerticalAlignment;
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextAlignment))
            {
                _textBlock.SetConst(dsShape.Container, TextBlock.TextAlignmentProperty, dsShape.TextAlignment);
                var viewBox = Content as Viewbox;
                if (viewBox is not null) SetContentHorizontalAlignment(viewBox, dsShape.TextAlignment);
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextWrapping))
            {
                _textBlock.SetConst(dsShape.Container, TextBlock.TextWrappingProperty, dsShape.TextWrapping);
                if (!VisualDesignMode)
                    if (dsShape.TextWrapping == TextWrapping.NoWrap &&
                        dsShape.TextAlignment == TextAlignment.Left &&
                        dsShape.TextStretch == Stretch.None &&
                        dsShape.AngleDeltaInfo.IsConst && dsShape.AngleInitial == 0.0 &&
                        !dsShape.IsFlipped)
                        Width = double.NaN;
            }
        }

        #endregion

        #region private functions

        private static void SetContentHorizontalAlignment(Viewbox viewBox, TextAlignment textAlignment)
        {
            switch (textAlignment)
            {
                case TextAlignment.Left:
                    viewBox.HorizontalAlignment = HorizontalAlignment.Left;
                    break;
                case TextAlignment.Center:
                    viewBox.HorizontalAlignment = HorizontalAlignment.Center;
                    break;
                case TextAlignment.Right:
                    viewBox.HorizontalAlignment = HorizontalAlignment.Right;
                    break;
                case TextAlignment.Justify:
                    viewBox.HorizontalAlignment = HorizontalAlignment.Stretch;
                    break;
            }
        }

        #endregion

        #region private fields

        private readonly TextBlock _textBlock;

        #endregion
    }
}