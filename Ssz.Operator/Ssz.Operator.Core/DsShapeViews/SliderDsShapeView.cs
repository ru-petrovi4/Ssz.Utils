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
    public class SliderDsShapeView : ControlDsShapeView<Slider>
    {
        #region public functions

        public static readonly DependencyProperty ThumbContentProperty =
            DependencyProperty.Register("ThumbContent", typeof(object), typeof(SliderDsShapeView),
                new PropertyMetadata(null));

        #endregion

        #region private fields

        private BindingExpressionBase? _valueBindingExpression;

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

        #region construction and destruction

        public SliderDsShapeView(SliderDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new Slider(), dsShape, frame)
        {
            if (!VisualDesignMode)
            {
                Control.PreviewMouseDown += SliderMouseDown;
                Control.PreviewMouseUp += SliderMouseUp;
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                if (!VisualDesignMode)
                {
                    Control.PreviewMouseDown -= SliderMouseDown;
                    Control.PreviewMouseUp -= SliderMouseUp;
                }

            // Release unmanaged resources.
            // Set large fields to null.            
            base.Dispose(disposing);
        }

        #endregion

        #region private functions

        private void SliderMouseDown(object? sender, MouseButtonEventArgs e)
        {
            if (_valueBindingExpression is not null)
            {
                var valueConverter =
                    (ValueConverterBase) ((MultiBinding) _valueBindingExpression.ParentBindingBase).Converter;
                valueConverter.DisableUpdatingTarget = true;
            }
        }

        private void SliderMouseUp(object? sender, MouseButtonEventArgs e)
        {
            if (_valueBindingExpression is not null)
            {
                var valueConverter =
                    (ValueConverterBase) ((MultiBinding) _valueBindingExpression.ParentBindingBase).Converter;
                _valueBindingExpression.UpdateSource();
                Dispatcher.BeginInvoke(new Action(() => { valueConverter.DisableUpdatingTarget = false; }));
            }
        }

        #endregion
    }
}