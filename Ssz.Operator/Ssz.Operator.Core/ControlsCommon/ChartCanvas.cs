using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsCommon.Converters;



using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsCommon
{
    public class ChartCanvas : Canvas, IDisposable
    {
        #region construction and destruction

        public ChartCanvas(MultiChartDsShapeView multiChartDsShapeView, MultiDsChartItem multiDsChartItem,
            bool visualDesignMode)
        {
            _multiChartDsShapeView = multiChartDsShapeView;
            _multiDsChartItem = multiDsChartItem;
            _visualDesignMode = visualDesignMode;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
            }

            Disposed = true;
        }

        ~ChartCanvas()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty PointsCountProperty = DependencyProperty.Register(
            @"PointsCount",
            typeof(int),
            typeof(ChartCanvas),
            new FrameworkPropertyMetadata(0, PointsCountPropertyOnChanged));

        public bool Disposed { get; private set; }

        #endregion

        #region private functions

        private static void PointsCountPropertyOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var thisChartCanvas = (ChartCanvas) d;

            thisChartCanvas.Refresh();
        }

        private void Refresh()
        {
            Children.Clear();

            var pointsCount = (int) GetValue(PointsCountProperty);

            if (pointsCount < 1 || pointsCount > 0xFFFF) return;

            var dsShape = (MultiChartDsShape) _multiChartDsShapeView.DsShapeViewModel.DsShape;
            var genericContainer = new GenericContainer();
            genericContainer.ParentItem = dsShape.Container;
            var gpi = new DsConstant
            {
                Name = MultiDsChartItem.PointNumberConstantConst
            };
            genericContainer.DsConstantsCollection.Add(gpi);

            var pointGeometry = GeometryHelper.GetGeometry(_multiDsChartItem.PointGeometryInfo,
                new ChartItemPointGeometryInfoSupplier(), dsShape.Container);
            if (pointGeometry is null) return;

            // Lines prepare

            PathFigure? linePathFigure = null;
            PathSegment? linePathSegment = null;
            MultiBinding? linePointsCollectionBinding = null;
            if (_multiDsChartItem.LineIsVisible)
            {
                ChildrenAddLinePath(out linePathFigure, out linePathSegment);

                linePointsCollectionBinding = new MultiBinding
                {
                    Mode = BindingMode.OneWay,
                    Converter = new PointsCollectionValueConverter()
                };
            }

            // Main loop

            Path? prevPointPath = null;
            var pointsStartNumber = _multiDsChartItem.PointsStartNumber;
            var pointNumberFormat =
                ConstantsHelper.ComputeValue(dsShape.Container, _multiDsChartItem.PointNumberFormat);
            for (var index = 0; index < pointsCount; index += 1)
            {
                gpi.Value = (pointsStartNumber + index).ToString(pointNumberFormat);

                Path pointPath = ChildrenAddPointPath(index, pointsCount, pointGeometry, genericContainer);

                // Lines

                if (_multiDsChartItem.LineIsVisible)
                {
                    if (index == 0)
                    {
                        MultiBinding pointMultiBinding = new()
                        {
                            Mode = BindingMode.OneWay
                        };
                        pointMultiBinding.Bindings.Add(new Binding
                        {
                            Source = pointPath,
                            Path = new PropertyPath(LeftProperty),
                            Mode = BindingMode.OneWay
                        });
                        pointMultiBinding.Bindings.Add(new Binding
                        {
                            Source = pointPath,
                            Path = new PropertyPath(TopProperty),
                            Mode = BindingMode.OneWay
                        });
                        pointMultiBinding.Converter = new PointValueConverter();

                        BindingOperations.SetBinding(linePathFigure, PathFigure.StartPointProperty, pointMultiBinding);
                    }
                    else
                    {
                        if (linePointsCollectionBinding is null) throw new InvalidOperationException();
                        linePointsCollectionBinding.Bindings.Add(new Binding
                        {
                            Source = pointPath,
                            Path = new PropertyPath(LeftProperty),
                            Mode = BindingMode.OneWay
                        });
                        linePointsCollectionBinding.Bindings.Add(new Binding
                        {
                            Source = pointPath,
                            Path = new PropertyPath(TopProperty),
                            Mode = BindingMode.OneWay
                        });
                    }
                }

                // Fill

                if (_multiDsChartItem.FillIsVisible && prevPointPath is not null)
                    ChildrenAddFillPolygon(genericContainer, pointPath, prevPointPath);

                prevPointPath = pointPath;
            }

            if (_multiDsChartItem.LineIsVisible)
                switch (_multiDsChartItem.Type)
                {
                    case MultiDsChartItem.AppearanceType.Line:
                        BindingOperations.SetBinding(linePathSegment, PolyLineSegment.PointsProperty,
                            linePointsCollectionBinding);
                        break;
                    case MultiDsChartItem.AppearanceType.Bezier:
                        BindingOperations.SetBinding(linePathSegment, PolyBezierSegment.PointsProperty,
                            linePointsCollectionBinding);
                        break;
                    case MultiDsChartItem.AppearanceType.QuadraticBezier:
                        BindingOperations.SetBinding(linePathSegment, PolyQuadraticBezierSegment.PointsProperty,
                            linePointsCollectionBinding);
                        break;
                }
        }


        private Path ChildrenAddPointPath(int index, int pointsCount, Geometry pointGeometry,
            GenericContainer genericContainer)
        {
            var pointPath = new Path
            {
                Data = pointGeometry,
                Stretch = Stretch.None
            };
            pointPath.ToolTip = "";
            pointPath.ToolTipOpening += PathOnToolTipOpening;
            Children.Add(pointPath);
            pointPath.SetBindingOrConst(genericContainer, Shape.StrokeProperty, _multiDsChartItem.PointDsBrush,
                BindingMode.OneWay,
                UpdateSourceTrigger.Default, _visualDesignMode);
            if (_multiDsChartItem.PointsIsVisible)
                pointPath.StrokeThickness = _multiDsChartItem.PointStrokeThickness;
            else
                pointPath.StrokeThickness = 0.0;

            // pointPath LeftProperty binding

            BindingBase leftBinding;
            if (_multiDsChartItem.PointValueXInfo.IsConst)
            {
                double xMultiplier;
                if (pointsCount > 1) xMultiplier = index / (double) (pointsCount - 1);
                else xMultiplier = 0.5;
                leftBinding = new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(ActualWidth)),
                    Mode = BindingMode.OneWay,
                    Converter = new ConstMultiplicationValueConverter {Multiplier = xMultiplier}
                };
            }
            else
            {
                MultiBinding leftMultiBinding = DependencyObjectExtention.CreateBindingWithoutConverter(
                    genericContainer,
                    _multiDsChartItem.PointValueXInfo, BindingMode.OneWay,
                    UpdateSourceTrigger.Default);
                leftMultiBinding.Bindings.Add(new Binding
                {
                    Source = _multiChartDsShapeView.AxisXTopControl,
                    Path = new PropertyPath(nameof(TextTickBar.Maximum)),
                    Mode = BindingMode.OneWay
                });
                leftMultiBinding.Bindings.Add(new Binding
                {
                    Source = _multiChartDsShapeView.AxisXTopControl,
                    Path = new PropertyPath(nameof(TextTickBar.Minimum)),
                    Mode = BindingMode.OneWay
                });
                leftMultiBinding.Bindings.Add(new Binding
                {
                    Source = this,
                    Path = new PropertyPath(nameof(ActualWidth)),
                    Mode = BindingMode.OneWay
                });
                var multiValueConverter = new LinearMultiValueConverter
                {
                    Bias = 0,
                    Gain = 1
                };
                if (_multiDsChartItem.PointValueXInfo.IsConst || _visualDesignMode)
                    multiValueConverter.ConstValue = _multiDsChartItem.PointValueXInfo.ConstValue;
                else
                    multiValueConverter.ValueConverter =
                        _multiDsChartItem.PointValueXInfo.GetConverterOrDefaultConverter(genericContainer);
                leftMultiBinding.Converter = multiValueConverter;
                leftBinding = leftMultiBinding;
            }

            BindingOperations.SetBinding(pointPath, LeftProperty, leftBinding);
            DependencyObjectExtention.RegisterMultiBinding(pointPath, leftBinding as MultiBinding);

            // pointPath TopProperty binding

            MultiBinding topMultiBinding = DependencyObjectExtention.CreateBindingWithoutConverter(
                genericContainer,
                _multiDsChartItem.PointValueYInfo, BindingMode.OneWay,
                UpdateSourceTrigger.Default);
            topMultiBinding.Bindings.Add(new Binding
            {
                Source = _multiChartDsShapeView.AxisYLeftControl,
                Path = new PropertyPath(nameof(TextTickBar.Maximum)),
                Mode = BindingMode.OneWay
            });
            topMultiBinding.Bindings.Add(new Binding
            {
                Source = _multiChartDsShapeView.AxisYLeftControl,
                Path = new PropertyPath(nameof(TextTickBar.Minimum)),
                Mode = BindingMode.OneWay
            });
            topMultiBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = new PropertyPath(nameof(ActualHeight)),
                Mode = BindingMode.OneWay
            });
            var yMultiValueConverter = new LinearMultiValueConverter
            {
                Bias = 1,
                Gain = -1
            };
            if (_multiDsChartItem.PointValueYInfo.IsConst || _visualDesignMode)
                yMultiValueConverter.ConstValue = _multiDsChartItem.PointValueYInfo.ConstValue;
            else
                yMultiValueConverter.ValueConverter =
                    _multiDsChartItem.PointValueYInfo.GetConverterOrDefaultConverter(genericContainer);
            topMultiBinding.Converter = yMultiValueConverter;
            BindingOperations.SetBinding(pointPath, TopProperty, topMultiBinding);
            DependencyObjectExtention.RegisterMultiBinding(pointPath, topMultiBinding);

            return pointPath;
        }

        private void ChildrenAddLinePath(out PathFigure linePathFigure, out PathSegment? linePathSegment)
        {
            var dsShape = (MultiChartDsShape) _multiChartDsShapeView.DsShapeViewModel.DsShape;

            switch (_multiDsChartItem.Type)
            {
                case MultiDsChartItem.AppearanceType.Line:
                    linePathSegment = new PolyLineSegment();
                    break;
                case MultiDsChartItem.AppearanceType.Bezier:
                    linePathSegment = new PolyBezierSegment();
                    break;
                case MultiDsChartItem.AppearanceType.QuadraticBezier:
                    linePathSegment = new PolyQuadraticBezierSegment();
                    break;
                default:
                    linePathSegment = null;
                    break;
            }

            linePathFigure = new PathFigure();
            var linePathGeometry = new PathGeometry();
            var linePath = new Path();
            var toolTipLinePath = new Path();
            toolTipLinePath.ToolTip = "";
            toolTipLinePath.ToolTipOpening += PathOnToolTipOpening;

            linePathFigure.Segments.Add(linePathSegment);
            linePathGeometry.Figures.Add(linePathFigure);
            linePath.Data = linePathGeometry;
            toolTipLinePath.Data = linePathGeometry;
            Children.Add(linePath);
            Children.Add(toolTipLinePath);

            linePath.SetBindingOrConst(dsShape.Container, Shape.StrokeProperty, _multiDsChartItem.LineDsBrush,
                BindingMode.OneWay,
                UpdateSourceTrigger.Default, _visualDesignMode);
            toolTipLinePath.Stroke = Brushes.Transparent;
            linePath.StrokeThickness = _multiDsChartItem.LineStrokeThickness;
            toolTipLinePath.StrokeThickness = _multiDsChartItem.LineStrokeThickness + 4;
            if (_multiDsChartItem.LineStrokeDashLength.HasValue && _multiDsChartItem.LineStrokeGapLength.HasValue)
            {
                linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
                    new DoubleCollection(new[]
                    {
                        _multiDsChartItem.LineStrokeDashLength.Value,
                        _multiDsChartItem.LineStrokeGapLength.Value
                    }));
            }
            else
            {
                if (!string.IsNullOrEmpty(_multiDsChartItem.LineStrokeDashArray))
                {
                    var a = _multiDsChartItem.LineStrokeDashArray.Split(',');
                    if (a.Length > 1)
                        linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
                            new DoubleCollection(a.Select(s => ObsoleteAnyHelper.ConvertTo<double>(s, false))));
                    else
                        linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
                }
                else
                {
                    linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
                }
            }
        }


        private void ChildrenAddFillPolygon(GenericContainer genericContainer, Path pointPath, Path prevPointPath)
        {
            MultiBinding polygonPointsCollectionBinding = new()
            {
                Mode = BindingMode.OneWay
            };
            polygonPointsCollectionBinding.Bindings.Add(new Binding
            {
                Source = prevPointPath,
                Path = new PropertyPath(LeftProperty),
                Mode = BindingMode.OneWay
            });
            polygonPointsCollectionBinding.Bindings.Add(new Binding
            {
                Source = prevPointPath,
                Path = new PropertyPath(TopProperty),
                Mode = BindingMode.OneWay
            });
            polygonPointsCollectionBinding.Bindings.Add(new Binding
            {
                Source = pointPath,
                Path = new PropertyPath(LeftProperty),
                Mode = BindingMode.OneWay
            });
            polygonPointsCollectionBinding.Bindings.Add(new Binding
            {
                Source = pointPath,
                Path = new PropertyPath(TopProperty),
                Mode = BindingMode.OneWay
            });
            polygonPointsCollectionBinding.Bindings.Add(new Binding
            {
                Source = this,
                Path = new PropertyPath(ActualHeightProperty),
                Mode = BindingMode.OneWay
            });
            polygonPointsCollectionBinding.Converter = new PolygonPointsCollectionValueConverter();

            var fillPolygon = new Polygon();
            fillPolygon.StrokeThickness = 1;
            fillPolygon.SetBindingOrConst(genericContainer, Shape.StrokeProperty, _multiDsChartItem.FillDsBrush,
                BindingMode.OneWay,
                UpdateSourceTrigger.Default, _visualDesignMode);
            fillPolygon.SetBindingOrConst(genericContainer, Shape.FillProperty, _multiDsChartItem.FillDsBrush,
                BindingMode.OneWay,
                UpdateSourceTrigger.Default, _visualDesignMode);
            Children.Add(fillPolygon);
            SetZIndex(fillPolygon, -1);
            BindingOperations.SetBinding(fillPolygon, Polygon.PointsProperty, polygonPointsCollectionBinding);
        }

        private void PathOnToolTipOpening(object? sender, ToolTipEventArgs e)
        {
            if (sender is null) return;
            ((Path) sender).ToolTip = GetToolTip();
        }

        private string GetToolTip()
        {
            var p = Mouse.GetPosition(this);

            var x = _multiChartDsShapeView.AxisXTopControl.Minimum +
                    (_multiChartDsShapeView.AxisXTopControl.Maximum -
                     _multiChartDsShapeView.AxisXTopControl.Minimum) * p.X / ActualWidth;

            var y = _multiChartDsShapeView.AxisYLeftControl.Minimum +
                    (_multiChartDsShapeView.AxisYLeftControl.Maximum -
                     _multiChartDsShapeView.AxisYLeftControl.Minimum) * (ActualHeight - p.Y) / ActualHeight;

            return _multiChartDsShapeView.AxisXTopControl.GetValueWithEngUnit(x) +
                   CultureInfo.CurrentCulture.TextInfo.ListSeparator + " " +
                   _multiChartDsShapeView.AxisYLeftControl.GetValueWithEngUnit(y);
        }

        #endregion

        #region private fields

        private readonly MultiChartDsShapeView _multiChartDsShapeView;

        private readonly MultiDsChartItem _multiDsChartItem;

        private readonly bool _visualDesignMode;

        #endregion

        private class LinearMultiValueConverter : IMultiValueConverter, IDisposable
        {
            #region construction and destruction

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }


            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                    if (ValueConverter is not null)
                        ValueConverter.Dispose();

                ValueConverter = null;

                Disposed = true;
            }


            ~LinearMultiValueConverter()
            {
                Dispose(false);
            }

            #endregion

            #region public functions

            public bool Disposed { get; private set; }


            public ValueConverterBase? ValueConverter { get; set; }

            public double ConstValue { get; set; }

            public double Bias { get; set; }

            public double Gain { get; set; }

            public object? Convert(object?[]? values, Type? targetType, object? parameter,
                CultureInfo culture)
            {
                if (values is null) return Binding.DoNothing;
                var length = values.Length;
                if (length < 3) return Binding.DoNothing;
                double value;
                if (ValueConverter is null)
                {
                    value = ConstValue;
                }
                else
                {
                    var oValue = ValueConverter.Convert(values, typeof(double), parameter, culture);
                    if (!(oValue is double)) return Binding.DoNothing;
                    value = (double)oValue;
                }

                var maximum = values[length - 3];
                var minimum = values[length - 2];
                var k = values[length - 1];
                if (!(maximum is double) || !(minimum is double) || !(k is double)) return Binding.DoNothing;
                var result = (Bias + Gain * (value - (double)minimum) / ((double)maximum - (double)minimum)) *
                             (double)k;
                if (double.IsNaN(result) || double.IsInfinity(result)) return Binding.DoNothing;
                return result;
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
                CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private class PointValueConverter : IMultiValueConverter, IDisposable
        {
            #region construction and destruction

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }


            protected virtual void Dispose(bool disposing)
            {
                Disposed = true;
            }


            ~PointValueConverter()
            {
                Dispose(false);
            }

            #endregion

            #region public functions

            public bool Disposed { get; private set; }

            public object? Convert(object?[]? values, Type? targetType, object? parameter,
                CultureInfo culture)
            {
                if (values is null || values.Length < 2) return Binding.DoNothing;

                var xValue = values[0];
                var yValue = values[1];
                if (xValue is double && yValue is double) return new Point((double)xValue, (double)yValue);
                return Binding.DoNothing;
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
                CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private class PolygonPointsCollectionValueConverter : IMultiValueConverter, IDisposable
        {
            #region construction and destruction

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }


            protected virtual void Dispose(bool disposing)
            {
                Disposed = true;
            }


            ~PolygonPointsCollectionValueConverter()
            {
                Dispose(false);
            }

            #endregion

            #region public functions

            public bool Disposed { get; private set; }

            public object? Convert(object?[]? values, Type? targetType, object? parameter,
                CultureInfo culture)
            {
                if (values is null || values.Length < 5) return Binding.DoNothing;

                var prevXValue = values[0];
                var prevYValue = values[1];
                var xValue = values[2];
                var yValue = values[3];
                var height = values[4];
                if (prevXValue is double && prevYValue is double && xValue is double && yValue is double &&
                    height is double)
                {
                    var poinsCollection = new PointCollection(4);
                    poinsCollection.Add(new Point((double)prevXValue, (double)prevYValue));
                    poinsCollection.Add(new Point((double)xValue, (double)yValue));
                    poinsCollection.Add(new Point((double)xValue, (double)height));
                    poinsCollection.Add(new Point((double)prevXValue, (double)height));
                    return poinsCollection;
                }

                return Binding.DoNothing;
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
                CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        private class PointsCollectionValueConverter : IMultiValueConverter, IDisposable
        {
            #region construction and destruction

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }


            protected virtual void Dispose(bool disposing)
            {
                Disposed = true;
            }


            ~PointsCollectionValueConverter()
            {
                Dispose(false);
            }

            #endregion

            #region public functions

            public bool Disposed { get; private set; }

            public object? Convert(object?[]? values, Type? targetType, object? parameter,
                CultureInfo culture)
            {
                if (values is null || values.Length < 2) return Binding.DoNothing;

                var poinsCollection = new PointCollection();
                for (var p = 0; p < values.Length / 2; p += 1)
                {
                    var xValue = values[p * 2];
                    var yValue = values[p * 2 + 1];
                    if (xValue is double && yValue is double)
                        poinsCollection.Add(new Point((double)xValue, (double)yValue));
                }

                return poinsCollection;
            }

            public object?[] ConvertBack(object? value, Type?[] targetTypes, object? parameter,
                CultureInfo culture)
            {
                throw new NotImplementedException();
            }

            #endregion
        }
    }
}