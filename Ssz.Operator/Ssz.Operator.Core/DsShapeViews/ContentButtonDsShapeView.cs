using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ContentButtonDsShapeView : ButtonDsShapeViewBase
    {
        #region construction and destruction

        public ContentButtonDsShapeView(ContentButtonDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            Control.SnapsToDevicePixels = false;
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = (ContentButtonDsShape) DsShapeViewModel.DsShape;
            if (propertyName is null || propertyName == nameof(dsShape.ContentInfo)
                                     || propertyName == nameof(dsShape.PressedContentInfo))
            {
                if (VisualDesignMode ||
                    dsShape.PressedContentInfo.IsConst && dsShape.PressedContentInfo.ConstValue.IsEmpty)
                {
                    BindingOperations.ClearBinding(this, UnPressedContentProperty);

                    BindingOperations.ClearBinding(this, PressedContentProperty);

                    Control.SetBindingOrConst(dsShape.Container, ContentControl.ContentProperty,
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
                        Path = new PropertyPath("UnPressedContent"),
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
                    multiBinding.Converter = new ContentConverter();
                    BindingOperations.SetBinding(Control, ContentControl.ContentProperty, multiBinding);
                }
            }
        }

        #endregion

        private class ContentConverter : IMultiValueConverter
        {
            #region public functions

            public object? Convert(object?[]? values, Type? targetType, object? parameter,
                CultureInfo culture)
            {
                if (values is null || values.Length != 3 || values[2] is null) return Binding.DoNothing;
                if ((bool) (values[2] ?? throw new InvalidOperationException())) return values[1];
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

        public static readonly DependencyProperty UnPressedContentProperty = DependencyProperty.Register(
            "UnPressedContent",
            typeof(object),
            typeof(ContentButtonDsShapeView),
            new FrameworkPropertyMetadata(null));

        public static readonly DependencyProperty PressedContentProperty = DependencyProperty.Register(
            "PressedContent",
            typeof(object),
            typeof(ContentButtonDsShapeView),
            new FrameworkPropertyMetadata(null));

        #endregion
    }
}