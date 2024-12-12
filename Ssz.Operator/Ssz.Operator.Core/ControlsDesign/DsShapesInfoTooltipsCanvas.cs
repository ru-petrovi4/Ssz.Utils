using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;

namespace Ssz.Operator.Core.ControlsDesign
{
    public class DsShapesInfoTooltipsCanvas : Canvas
    {
        #region private fields

        private readonly DesignDrawingCanvas _parentCanvas;

        #endregion

        #region construction and destruction

        public DsShapesInfoTooltipsCanvas(DesignDrawingCanvas parentCanvas)
        {
            _parentCanvas = parentCanvas;

            SetBinding(ShowDsShapesInfoTooltipsProperty, new Binding
            {
                Source = DesignDsProjectViewModel.Instance,
                Path = new PropertyPath(
                    @"ShowDsShapesInfoTooltips"),
                Mode = BindingMode.OneWay
            });
        }

        #endregion

        #region private functions

        private static void OnShowDsShapesInfoTooltipsChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var thisCanvas = d as DsShapesInfoTooltipsCanvas;
            if (thisCanvas is null) return;
            thisCanvas.Refresh((bool) e.NewValue);
        }

        #endregion

        private class ViewScaleToFontSizeConverter : IMultiValueConverter
        {
            #region public functions

            public object? Convert(object?[]? values, Type? targetType, object? parameter, CultureInfo culture)
            {
                if (values is null || values.Length != 2) return Binding.DoNothing;

                return 18 / (double) (values[0] ?? throw new InvalidOperationException()) *
                       (0.2 + (double) (values[1] ?? throw new InvalidOperationException()));
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        #region public functions

        public static readonly DependencyProperty ShowDsShapesInfoTooltipsProperty =
            DependencyProperty.Register("ShowDsShapesInfoTooltips",
                typeof(
                    bool),
                typeof(DsShapesInfoTooltipsCanvas),
                new PropertyMetadata(false, OnShowDsShapesInfoTooltipsChanged));

        public void Refresh(bool showDsShapesInfoTooltips)
        {
            if (showDsShapesInfoTooltips)
            {
                Children.Clear();

                foreach (DsShapeViewModel dsShapeViewModel in _parentCanvas.DesignDrawingViewModel
                    .GetRootDsShapeViewModels())
                {
                    var complexDsShape = dsShapeViewModel.DsShape as ComplexDsShape;

                    if (complexDsShape is not null && complexDsShape.DsConstantsCollection.Count > 0)
                    {
                        StringBuilder text = new();
                        foreach (
                            DsConstant gpi in
                            complexDsShape.DsConstantsCollection)
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

                        BindingOperations.SetBinding(textBox, OpacityProperty, new Binding
                        {
                            Source = DesignDsProjectViewModel.Instance,
                            Path =
                                new PropertyPath(nameof(DesignDsProjectViewModel.DsShapesInfoOpacity)),
                            Mode = BindingMode.OneWay
                        });

                        SetLeft(textBox, dsShapeViewModel.DsShape.GetBoundingRect().Left);
                        SetTop(textBox, dsShapeViewModel.DsShape.GetBoundingRect().Top);
                        /*
                        BindingOperations.SetBinding(textBox, LeftProperty, new Binding
                        {
                            Source = dsShapeViewModel,
                            Path =
                                new PropertyPath(@"CenterInitialPositionX"),
                            Mode = BindingMode.OneWay
                        });

                        BindingOperations.SetBinding(textBox, TopProperty, new Binding
                        {
                            Source = dsShapeViewModel,
                            Path =
                                new PropertyPath(@"CenterInitialPositionY"),
                            Mode = BindingMode.OneWay
                        });*/

                        var multiBinding = new MultiBinding();
                        multiBinding.Bindings.Add(new Binding
                        {
                            Source = DesignDsProjectViewModel.Instance,
                            Path = new PropertyPath(nameof(DesignDsProjectViewModel.DesignDrawingViewScale)),
                            Mode = BindingMode.OneWay
                        });
                        multiBinding.Bindings.Add(new Binding
                        {
                            Source = DesignDsProjectViewModel.Instance,
                            Path = new PropertyPath(nameof(DesignDsProjectViewModel.DsShapesInfoFontSizeScale)),
                            Mode = BindingMode.OneWay
                        });
                        multiBinding.Converter = new ViewScaleToFontSizeConverter();
                        BindingOperations.SetBinding(textBox, Control.FontSizeProperty, multiBinding);

                        SetZIndex(textBox, dsShapeViewModel.ZIndex);

                        Children.Add(textBox);
                    }
                }
            }
            else
            {
                Children.Clear();
            }
        }

        #endregion
    }
}