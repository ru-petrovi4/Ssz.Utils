using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
            Control.SnapsToDevicePixels = false;
            Control.Focusable = false;

            BindingOperations.SetBinding(Control, ToggleButton.IsCheckedProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath("IsChecked"),
                Mode = BindingMode.TwoWay
            });

            var multiBinding = new MultiBinding();
            multiBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = new PropertyPath("UncheckedContent"),
                Mode = BindingMode.OneWay
            });
            multiBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = new PropertyPath("CheckedContent"),
                Mode = BindingMode.OneWay
            });
            multiBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = new PropertyPath("PressedContent"),
                Mode = BindingMode.OneWay
            });
            multiBinding.Bindings.Add(new Binding
            {
                Source = Control,
                Path = new PropertyPath("IsPressed"),
                Mode = BindingMode.OneWay
            });
            multiBinding.Bindings.Add(new Binding
            {
                Source = Control,
                Path = new PropertyPath("IsChecked"),
                Mode = BindingMode.OneWay
            });
            multiBinding.Converter = new ContentConverter();
            BindingOperations.SetBinding(Control, ContentControl.ContentProperty, multiBinding);
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ToggleButtonDsShape) DsShapeViewModel.DsShape;
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

            public object? Convert(object?[]? values, Type? targetType, object? parameter,
                CultureInfo culture)
            {
                if (values is null || values.Length != 5) return Binding.DoNothing;
                if ((bool) (values[3] ?? throw new InvalidOperationException())) // Is Pressed
                {
                    if (values[2] is not null) return values[2];
                    return Binding.DoNothing;
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

        public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
            "IsChecked",
            typeof(bool),
            typeof(ToggleButtonDsShapeView),
            new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty UncheckedContentProperty = DependencyProperty.Register(
            "UncheckedContent",
            typeof(object),
            typeof(ToggleButtonDsShapeView),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty CheckedContentProperty = DependencyProperty.Register(
            "CheckedContent",
            typeof(object),
            typeof(ToggleButtonDsShapeView),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty PressedContentProperty = DependencyProperty.Register(
            "PressedContent",
            typeof(object),
            typeof(ToggleButtonDsShapeView),
            new FrameworkPropertyMetadata(null));

        #endregion
    }
}