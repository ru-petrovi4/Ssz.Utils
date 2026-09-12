using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

using Ssz.Operator.Core.ControlsCommon;
namespace Ssz.Operator.Core.DsShapeViews
{
    public class TextBlockButtonDsShapeView : ButtonDsShapeViewBase
    {
        #region private fields

        private readonly TextBlock _textBlock;

        #endregion

        #region construction and destruction

        public TextBlockButtonDsShapeView(TextBlockButtonDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            _textBlock = new TextBlock();
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (TextBlockButtonDsShape) DsShapeViewModel.DsShape;            
            if (propertyName is null || propertyName == nameof(dsShape.TextStretch))
            {
                if (dsShape.TextStretch == Stretch.None)
                {
                    var unclippedTextHost = Control.Content as UnclippedTextHost;
                    if (unclippedTextHost is null)
                    {
                        // Avalonia drops wrapped lines that do not fit the available
                        // height; UnclippedTextHost lays them all out, as WPF did.
                        unclippedTextHost = new UnclippedTextHost();
                        Control.Content = unclippedTextHost;
                        unclippedTextHost.Child = _textBlock;

                        Control.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                    }                    
                }
                else
                {
                    var viewBox = Control.Content as Viewbox;
                    if (viewBox is null)
                    {
                        viewBox = new Viewbox();
                        Control.Content = viewBox;
                        viewBox.Child = _textBlock;

                        SetHorizontalContentAlignment(Control, dsShape.TextAlignment);
                    }

                    viewBox.Stretch = dsShape.TextStretch;
                }
            }

            if (propertyName is null || propertyName == nameof(dsShape.BackgroundInfo))
                _textBlock.SetBindingOrConst(dsShape.Container, TextBlock.BackgroundProperty,
                    dsShape.BackgroundInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
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

                _textBlock.SetConst(dsShape.Container, TextBlock.FontFamilyProperty,
                    fontFamily ?? WpfTextDefaults.FontFamily);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontSizeProperty,
                    fontSize > 0.0 ? fontSize : WpfTextDefaults.FontSize);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontStyleProperty, fontStyle);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontStretchProperty, fontStretch);
                _textBlock.SetConst(dsShape.Container, TextBlock.FontWeightProperty, fontWeight);
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextAlignment))
            {
                _textBlock.SetConst(dsShape.Container, TextBlock.TextAlignmentProperty,
                    dsShape.TextAlignment);
                var viewBox = Control.Content as Viewbox;
                if (viewBox is not null) SetHorizontalContentAlignment(Control, dsShape.TextAlignment);
            }

            if (propertyName is null || propertyName == nameof(dsShape.TextWrapping))
                _textBlock.SetConst(dsShape.Container, TextBlock.TextWrappingProperty,
                    dsShape.TextWrapping);
        }

        #endregion

        #region private functions

        private static void SetHorizontalContentAlignment(Button button, TextAlignment textAlignment)
        {
            switch (textAlignment)
            {
                case TextAlignment.Left:
                    button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Left;
                    break;
                case TextAlignment.Center:
                    button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center;
                    break;
                case TextAlignment.Right:
                    button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                    break;
                case TextAlignment.Justify:
                    button.HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
                    break;
            }
        }

        #endregion
    }
}