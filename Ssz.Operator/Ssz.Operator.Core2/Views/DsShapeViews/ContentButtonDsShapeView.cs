using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
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
            //Control.SnapsToDevicePixels = false;
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
                    ClearValue(UnPressedContentProperty);

                    ClearValue(PressedContentProperty);

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
                    Control.Bind(ContentControl.ContentProperty, multiBinding);
                }
            }
        }

        #endregion

        private class ContentConverter : IMultiValueConverter
        {
            #region public functions

            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values is null || values.Count != 3 || values[2] is null) return BindingOperations.DoNothing;
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

        public static readonly AvaloniaProperty UnPressedContentProperty = AvaloniaProperty.Register<ContentButtonDsShapeView, object?>(
            "UnPressedContent",
            null);

        public static readonly AvaloniaProperty PressedContentProperty = AvaloniaProperty.Register<ContentButtonDsShapeView, object?>(
            "PressedContent",
            null);

        #endregion
    }
}