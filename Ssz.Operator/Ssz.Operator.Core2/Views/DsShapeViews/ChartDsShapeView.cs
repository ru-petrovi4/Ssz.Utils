using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Controls.Shapes;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Xceed.Wpf.Toolkit;
using Avalonia.Layout;
using Avalonia.Data;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class ChartDsShapeView : DsShapeViewBase
    {
        #region construction and destruction

        public ChartDsShapeView(ChartDsShape dsShape, ControlsPlay.Frame? frame)
            : base(dsShape, frame)
        {
            if (VisualDesignMode) 
                IsHitTestVisible = false;
        }

        #endregion

        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);            
            
            var dsShape = (ChartDsShape) DsShapeViewModel.DsShape;            

            switch (dsShape.Type)
            {
                case ChartDsShape.AppearanceType.StackedChart:
                    {
                        if (propertyName is null || propertyName == nameof(dsShape.Type))
                        {
                            Content = new Border();
                            propertyName = null; // Force refresh other props
                        }
                        var mainBorder = (Border)Content!;
                        if (propertyName is null || propertyName == nameof(dsShape.DsChartItemsCollection))
                        {
                            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
                            stackPanel.Width = 1.0;
                            stackPanel.Height = 1.0;
                            mainBorder.Child = new Viewbox
                            {
                                Stretch = Stretch.Fill,
                                Child = stackPanel
                            };

                            foreach (DsChartItem dsChartItem in dsShape.DsChartItemsCollection)
                            {
                                var rect = new Rectangle
                                {
                                    VerticalAlignment = VerticalAlignment.Stretch
                                };
                                stackPanel.Children.Add(rect);
                                rect.SetBindingOrConst(dsShape.Container, Shape.FillProperty, dsChartItem.DsBrush,
                                    BindingMode.OneWay,
                                    UpdateSourceTrigger.Default, VisualDesignMode);
                                rect.SetBindingOrConst(dsShape.Container, WidthProperty, dsChartItem.ValueInfo,
                                    BindingMode.OneWay,
                                    UpdateSourceTrigger.Default, VisualDesignMode);
                            }
                        }
                        if (propertyName is null || propertyName == nameof(dsShape.BackgroundInfo))
                            mainBorder.SetBindingOrConst(dsShape.Container, Border.BackgroundProperty, dsShape.BackgroundInfo,
                                BindingMode.OneWay,
                                UpdateSourceTrigger.Default, VisualDesignMode);
                        if (propertyName is null || propertyName == nameof(dsShape.BorderThickness))
                            mainBorder.SetConst(dsShape.Container, Border.BorderThicknessProperty, dsShape.BorderThickness);
                        if (propertyName is null || propertyName == nameof(dsShape.Padding))
                            mainBorder.SetConst(dsShape.Container, Border.PaddingProperty, dsShape.Padding);
                        if (propertyName is null || propertyName == nameof(dsShape.BorderBrushInfo))
                            mainBorder.SetBindingOrConst(dsShape.Container, Border.BorderBrushProperty, dsShape.BorderBrushInfo,
                                BindingMode.OneWay,
                                UpdateSourceTrigger.Default, VisualDesignMode);
                    }
                    break;
                //case ChartDsShape.AppearanceType.PieChart:
                //    {
                //        if (propertyName is null || propertyName == nameof(dsShape.Type))
                //        {
                //            Content = new Grid();
                //            propertyName = null; // Force refresh other props
                //        }
                //        var mainGrid = (Grid)Content!;
                //        if (propertyName is null || propertyName == nameof(dsShape.DsChartItemsCollection) || 
                //            propertyName == nameof(dsShape.BackgroundInfo))
                //        {
                //            mainGrid.Children.Clear();
                //            var prevPie = new Pie
                //            {
                //                Mode = PieMode.Slice,
                //                StartAngle = -90,
                //                Slice = 1
                //            };
                //            mainGrid.Children.Add(prevPie);
                //            prevPie.SetBindingOrConst(dsShape.Container, Shape.FillProperty, dsShape.BackgroundInfo,
                //                BindingMode.OneWay,
                //                UpdateSourceTrigger.Default, VisualDesignMode);
                //            foreach (DsChartItem dsChartItem in dsShape.DsChartItemsCollection)
                //            {
                //                var pie = new Pie
                //                {
                //                    Mode = PieMode.Slice
                //                };                                
                //                mainGrid.Children.Add(pie);
                //                pie.SetBindingOrConst(dsShape.Container, Shape.FillProperty, dsChartItem.DsBrush,
                //                    BindingMode.OneWay,
                //                    UpdateSourceTrigger.Default, VisualDesignMode);
                //                pie.SetBindingOrConst(dsShape.Container, Pie.SliceProperty, dsChartItem.ValueInfo,
                //                    BindingMode.OneWay,
                //                    UpdateSourceTrigger.Default, VisualDesignMode);
                //                BindingOperations.Bind(pie, Pie.StartAngleProperty, 
                //                    new Binding
                //                    {
                //                        Source = prevPie,
                //                        Path = new PropertyPath(nameof(Pie.EndAngle)),
                //                    });
                //                prevPie = pie;
                //            }
                //            propertyName = null; // Force refresh other props
                //        }                        
                //        if (propertyName is null || propertyName == nameof(dsShape.BorderThickness))
                //        {
                //            foreach (Pie pie in mainGrid.Children)
                //            {                                
                //                pie.SetConst(dsShape.Container, Shape.StrokeThicknessProperty, dsShape.BorderThickness.Left);
                //            }                            
                //        }
                //        //if (propertyName is null || propertyName == nameof(dsShape.Padding))
                //        //    mainBorder.SetConst(dsShape.Container, Border.PaddingProperty, dsShape.Padding);
                //        if (propertyName is null || propertyName == nameof(dsShape.BorderBrushInfo))
                //        {
                //            foreach (Pie pie in mainGrid.Children)
                //            {
                //                pie.SetBindingOrConst(dsShape.Container, Shape.StrokeProperty, dsShape.BorderBrushInfo,
                //                    BindingMode.OneWay,
                //                    UpdateSourceTrigger.Default, VisualDesignMode);
                //            }
                //        }
                //    }
                //    break;
                //case ChartDsShape.AppearanceType.RingChart:
                //    {
                //        if (propertyName is null || propertyName == nameof(dsShape.Type))
                //        {
                //            Content = new Grid();
                //            propertyName = null; // Force refresh other props
                //        }
                //        var mainGrid = (Grid)Content!;
                //        if (propertyName is null || propertyName == nameof(dsShape.DsChartItemsCollection) ||
                //            propertyName == nameof(dsShape.BackgroundInfo) ||
                //            propertyName == nameof(dsShape.BorderThickness))
                //        {
                //            mainGrid.Children.Clear();
                //            var prevPie = new Pie
                //            {
                //                Mode = PieMode.Slice,
                //                StartAngle = -90,
                //                StrokeThickness = 0,
                //                Slice = 1
                //            };
                //            mainGrid.Children.Add(prevPie);
                //            prevPie.SetBindingOrConst(dsShape.Container, Shape.FillProperty, dsShape.BorderBrushInfo,
                //                BindingMode.OneWay,
                //                UpdateSourceTrigger.Default, VisualDesignMode);
                //            foreach (DsChartItem dsChartItem in dsShape.DsChartItemsCollection)
                //            {
                //                var pie = new Pie
                //                {
                //                    Mode = PieMode.Slice,
                //                    StrokeThickness = 0,
                //                };
                //                mainGrid.Children.Add(pie);
                //                pie.SetBindingOrConst(dsShape.Container, Shape.FillProperty, dsChartItem.DsBrush,
                //                    BindingMode.OneWay,
                //                    UpdateSourceTrigger.Default, VisualDesignMode);
                //                pie.SetBindingOrConst(dsShape.Container, Pie.SliceProperty, dsChartItem.ValueInfo,
                //                    BindingMode.OneWay,
                //                    UpdateSourceTrigger.Default, VisualDesignMode);
                //                BindingOperations.Bind(pie, Pie.StartAngleProperty,
                //                    new Binding
                //                    {
                //                        Source = prevPie,
                //                        Path = new PropertyPath(nameof(Pie.EndAngle)),
                //                    });
                //                prevPie = pie;
                //            }
                //            var centralPie = new Pie
                //            {
                //                Mode = PieMode.Slice,
                //                StrokeThickness = 0,
                //                Slice = 1
                //            };
                //            mainGrid.Children.Add(centralPie);
                //            centralPie.SetBindingOrConst(dsShape.Container, Shape.FillProperty, dsShape.BackgroundInfo,
                //                BindingMode.OneWay,
                //                UpdateSourceTrigger.Default, VisualDesignMode);
                //            centralPie.SetConst(dsShape.Container, Control.MarginProperty, dsShape.BorderThickness);
                //            propertyName = null; // Force refresh other props
                //        }                        
                //        //if (propertyName is null || propertyName == nameof(dsShape.Padding))
                //        //    mainBorder.SetConst(dsShape.Container, Border.PaddingProperty, dsShape.Padding);
                //        //if (propertyName is null || propertyName == nameof(dsShape.BorderDsBrush))
                //        //{
                //        //    foreach (Pie pie in mainGrid.Children)
                //        //    {
                //        //        pie.SetBindingOrConst(dsShape.Container, Shape.StrokeProperty, dsShape.BorderDsBrush,
                //        //            BindingMode.OneWay,
                //        //            UpdateSourceTrigger.Default, VisualDesignMode);
                //        //    }
                //        //}
                //    }
                //    break;
            }           
        }

        #endregion
    }
}