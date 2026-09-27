using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     Shows, over each complex shape of the drawing, the constants it was given - which is how the
    ///     author checks what a reused shape was parameterised with without opening it.
    /// </summary>
    public class DsShapesInfoTooltipsCanvas : Canvas
    {
        #region construction and destruction

        public DsShapesInfoTooltipsCanvas(DesignDrawingCanvas parentCanvas)
        {
            _parentCanvas = parentCanvas;

            this.Bind(ShowDsShapesInfoTooltipsProperty, new Binding
            {
                Source = DesignDsProjectViewModel.Instance,
                Path = nameof(DesignDsProjectViewModel.ShowDsShapesInfoTooltips),
                Mode = BindingMode.OneWay
            });
        }

        #endregion

        #region public functions

        public static readonly StyledProperty<bool> ShowDsShapesInfoTooltipsProperty =
            AvaloniaProperty.Register<DsShapesInfoTooltipsCanvas, bool>(nameof(ShowDsShapesInfoTooltips));

        public bool ShowDsShapesInfoTooltips
        {
            get => GetValue(ShowDsShapesInfoTooltipsProperty);
            set => SetValue(ShowDsShapesInfoTooltipsProperty, value);
        }

        public void Refresh(bool showDsShapesInfoTooltips)
        {
            Children.Clear();

            if (!showDsShapesInfoTooltips) return;

            foreach (DsShapeViewModel dsShapeViewModel in _parentCanvas.DesignDrawingViewModel
                         .GetRootDsShapeViewModels())
            {
                var complexDsShape = dsShapeViewModel.DsShape as ComplexDsShape;

                if (complexDsShape is not null && complexDsShape.DsConstantsCollection.Count > 0)
                {
                    StringBuilder text = new();
                    foreach (DsConstant gpi in complexDsShape.DsConstantsCollection)
                    {
                        if (text.Length != 0) text.AppendLine();
                        text.Append(gpi.Name + @" = " + gpi.Value);
                    }

                    var textBox = new TextBox
                    {
                        Background = new SolidColorBrush(Colors.White),
                        Foreground = new SolidColorBrush(Colors.Black),
                        BorderThickness = new Thickness(0),
                        Text = text.ToString()
                    };

                    textBox.Bind(OpacityProperty, new Binding
                    {
                        Source = DesignDsProjectViewModel.Instance,
                        Path = nameof(DesignDsProjectViewModel.DsShapesInfoOpacity),
                        Mode = BindingMode.OneWay
                    });

                    SetLeft(textBox, dsShapeViewModel.DsShape.GetBoundingRect().Left);
                    SetTop(textBox, dsShapeViewModel.DsShape.GetBoundingRect().Top);

                    // The text keeps its size on screen whatever the drawing is zoomed to, and the
                    // author scales it from the ribbon on top of that.
                    var multiBinding = new MultiBinding
                    {
                        Converter = ViewScaleToFontSizeConverter.Instance
                    };
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = DesignDsProjectViewModel.Instance,
                        Path = nameof(DesignDsProjectViewModel.DesignDrawingViewScale),
                        Mode = BindingMode.OneWay
                    });
                    multiBinding.Bindings.Add(new Binding
                    {
                        Source = DesignDsProjectViewModel.Instance,
                        Path = nameof(DesignDsProjectViewModel.DsShapesInfoFontSizeScale),
                        Mode = BindingMode.OneWay
                    });
                    textBox.Bind(TemplatedControl.FontSizeProperty, multiBinding);

                    textBox.ZIndex = dsShapeViewModel.ZIndex;

                    Children.Add(textBox);
                }
            }
        }

        #endregion

        #region protected functions

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ShowDsShapesInfoTooltipsProperty)
                Refresh(change.GetNewValue<bool>());
        }

        #endregion

        #region private fields

        private readonly DesignDrawingCanvas _parentCanvas;

        #endregion

        private class ViewScaleToFontSizeConverter : IMultiValueConverter
        {
            public static readonly ViewScaleToFontSizeConverter Instance = new();

            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values.Count != 2) return BindingOperations.DoNothing;
                if (values[0] is not double viewScale || values[1] is not double fontSizeScale)
                    return BindingOperations.DoNothing;
                if (viewScale == 0.0) return BindingOperations.DoNothing;

                return 18 / viewScale * (0.2 + fontSizeScale);
            }
        }
    }
}
