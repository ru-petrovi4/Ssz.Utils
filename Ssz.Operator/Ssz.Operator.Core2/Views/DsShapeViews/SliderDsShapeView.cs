using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Threading;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.MultiValueConverters;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class SliderDsShapeView : ControlDsShapeView<Slider>
    {
        #region construction and destruction

        public SliderDsShapeView(SliderDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new Slider(), dsShape, frame)
        {
            if (!VisualDesignMode)
            {
                Control.PointerPressed += Slider_PointerPressed;
                Control.PointerReleased += Slider_PointerReleased;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                if (!VisualDesignMode)
                {
                    Control.PointerPressed -= Slider_PointerPressed;
                    Control.PointerReleased -= Slider_PointerReleased;
                }

            // Release unmanaged resources.
            // Set large fields to null.            
            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public static readonly AvaloniaProperty ThumbContentProperty =
            AvaloniaProperty.Register<SliderDsShapeView, object?>(
                "ThumbContent", 
                null);

        #endregion        

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (SliderDsShape) DsShapeViewModel.DsShape;            
            if (propertyName is null || propertyName == nameof(dsShape.MaximumInfo))
                Control.SetBindingOrConst(dsShape.Container, RangeBase.MaximumProperty, dsShape.MaximumInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.MinimumInfo))
                Control.SetBindingOrConst(dsShape.Container, RangeBase.MinimumProperty, dsShape.MinimumInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.ValueInfo))
                _valueBindingExpression = Control.SetBindingOrConst(dsShape.Container, RangeBase.ValueProperty,
                    dsShape.ValueInfo,
                    BindingMode.TwoWay,
                    UpdateSourceTrigger.Explicit, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.XamlInfo))
                this.SetBindingOrConst(dsShape.Container, ThumbContentProperty, dsShape.XamlInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.LargeChangePercent))
                Control.SetConst(dsShape.Container, RangeBase.LargeChangeProperty, dsShape.LargeChangePercent);
            if (propertyName is null || propertyName == nameof(dsShape.SmallChangePercent))
                Control.SetConst(dsShape.Container, RangeBase.SmallChangeProperty, dsShape.SmallChangePercent);
        }

        #endregion        

        #region private functions

        private void Slider_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_valueBindingExpression.Item2 is not null)
            {
                var valueConverter =
                    (ValueConverterBase) _valueBindingExpression.Item2.Converter!;
                valueConverter.DisableUpdatingTarget = true;
            }
        }

        private void Slider_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_valueBindingExpression.Item2 is not null)
            {
                var valueConverter = (ValueConverterBase)_valueBindingExpression.Item2.Converter!;
                valueConverter.ConvertBack(Control.Value, DsShapeViewModel, null, CultureInfo.InvariantCulture);
                Dispatcher.UIThread.InvokeAsync(new Action(() => { valueConverter.DisableUpdatingTarget = false; }));
            }
        }

        #endregion

        #region private fields

        private (BindingExpressionBase?, MultiBinding?) _valueBindingExpression;

        #endregion
    }
}