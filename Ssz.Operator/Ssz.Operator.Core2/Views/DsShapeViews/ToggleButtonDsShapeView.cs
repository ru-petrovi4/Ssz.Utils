using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ToggleButtonDsShapeView : ControlDsShapeView<ToggleButton>
    {
        #region construction and destruction

        public ToggleButtonDsShapeView(ToggleButtonDsShape dsShape, ControlsPlay.Frame? frame)
            : base(new ToggleButton(), dsShape, frame)
        {
            //Control.SnapsToDevicePixels = false;
            Control.Focusable = false;

            Control.Bind(ToggleButton.IsCheckedProperty, new Binding
            {
                Source = this,
                Path = "IsChecked",
                Mode = BindingMode.TwoWay
            });

            var multiBinding = new MultiBinding();
            multiBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = "UncheckedContent",
                Mode = BindingMode.OneWay
            });
            multiBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = "CheckedContent",
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
            multiBinding.Bindings.Add(new Binding
            {
                Source = Control,
                Path = "IsChecked",
                Mode = BindingMode.OneWay
            });
            multiBinding.Converter = new ContentConverter();
            Control.Bind(ContentControl.ContentProperty, multiBinding);
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ToggleButtonDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.HorizontalContentAlignment))
                Control.SetConst(dsShape.Container,
                    ToggleButton.HorizontalContentAlignmentProperty,
                    dsShape.HorizontalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.VerticalContentAlignment))
                Control.SetConst(dsShape.Container,
                    ToggleButton.VerticalContentAlignmentProperty,
                    dsShape.VerticalContentAlignment);
            if (propertyName is null || propertyName == nameof(dsShape.IsCheckedInfo))
                this.SetBindingOrConst(dsShape.Container, IsCheckedProperty, dsShape.IsCheckedInfo,
                    BindingMode.TwoWay, UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.UncheckedContentInfo))
                this.SetBindingOrConst(dsShape.Container, UncheckedContentProperty, dsShape.UncheckedContentInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.CheckedContentInfo))
                this.SetBindingOrConst(dsShape.Container, CheckedContentProperty, dsShape.CheckedContentInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
            if (propertyName is null || propertyName == nameof(dsShape.PressedContentInfo))
                this.SetBindingOrConst(dsShape.Container, PressedContentProperty, dsShape.PressedContentInfo,
                    BindingMode.OneWay,
                    UpdateSourceTrigger.Default, VisualDesignMode);
        }

        #endregion

        private class ContentConverter : IMultiValueConverter
        {
            #region public functions

            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values is null || values.Count != 5) return BindingOperations.DoNothing;
                if ((bool) (values[3] ?? throw new InvalidOperationException())) // Is Pressed
                {
                    if (values[2] is not null) return values[2];
                    return BindingOperations.DoNothing;
                }

                if ((bool?) values[4] == true) // Is Checked
                    return values[1];
                return values[0];
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
                CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #region public functions

        public static readonly AvaloniaProperty IsCheckedProperty = AvaloniaProperty.Register<ToggleButtonDsShapeView, bool>(
            "IsChecked",
            false);

        public static readonly AvaloniaProperty UncheckedContentProperty = AvaloniaProperty.Register<ToggleButtonDsShapeView, object?>(
            "UncheckedContent",
            null);

        public static readonly AvaloniaProperty CheckedContentProperty = AvaloniaProperty.Register<ToggleButtonDsShapeView, object?>(
            "CheckedContent",
            null);

        public static readonly AvaloniaProperty PressedContentProperty = AvaloniaProperty.Register<ToggleButtonDsShapeView, object?>(
            "PressedContent",
            null);

        #endregion
    }
}