using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
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
            Disposed = true;
        }

        ~ChartCanvas()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly StyledProperty<int> PointsCountProperty =
            AvaloniaProperty.Register<ChartCanvas, int>(nameof(PointsCount), defaultValue: 0);

        static ChartCanvas()
        {
            PointsCountProperty.Changed.AddClassHandler<ChartCanvas>((canvas, _) => canvas.Refresh());
        }

        public int PointsCount
        {
            get => GetValue(PointsCountProperty);
            set => SetValue(PointsCountProperty, value);
        }

        public bool Disposed { get; private set; }

        #endregion

        #region protected functions

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            var pos = e.GetPosition(this);
            _lastPointerX = pos.X;
            _lastPointerY = pos.Y;
        }

        #endregion

        #region private functions

        private void Refresh()
        {
            Children.Clear();

            var pointsCount = PointsCount;
            if (pointsCount < 1 || pointsCount > 0xFFFF) return;

            var dsShape = (MultiChartDsShape)_multiChartDsShapeView.DsShapeViewModel.DsShape;
            var genericContainer = new GenericContainer();
            genericContainer.ParentItem = dsShape.Container;
            var gpi = new DsConstant { Name = MultiDsChartItem.PointNumberConstantConst };
            genericContainer.DsConstantsCollection.Add(gpi);

            var pointGeometry = GeometryHelper.GetGeometry(
                _multiDsChartItem.PointGeometryInfo,
                new ChartItemPointGeometryInfoSupplier(),
                dsShape.Container);
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
            var pointNumberFormat = ConstantsHelper.ComputeValue(dsShape.Container, _multiDsChartItem.PointNumberFormat);

            for (var index = 0; index < pointsCount; index++)
            {
                gpi.Value = (pointsStartNumber + index).ToString(pointNumberFormat);
                Path pointPath = ChildrenAddPointPath(index, pointsCount, pointGeometry, genericContainer);

                // Lines
                if (_multiDsChartItem.LineIsVisible)
                {
                    if (index == 0)
                    {
                        // Avalonia: используем GetObservable для привязки к Canvas.Left/Top другого элемента
                        var startPointMultiBinding = new MultiBinding
                        {
                            Mode = BindingMode.OneWay,
                            Converter = new PointValueConverter()
                        };
                        startPointMultiBinding.Bindings.Add(
                            pointPath.GetObservable(Canvas.LeftProperty).ToBinding());
                        startPointMultiBinding.Bindings.Add(
                            pointPath.GetObservable(Canvas.TopProperty).ToBinding());

                        linePathFigure!.Bind(PathFigure.StartPointProperty, startPointMultiBinding);
                    }
                    else
                    {
                        if (linePointsCollectionBinding is null) throw new InvalidOperationException();
                        // Avalonia: GetObservable вместо Binding { Source = ..., Path = PropertyPath(Canvas.LeftProperty) }
                        linePointsCollectionBinding.Bindings.Add(
                            pointPath.GetObservable(Canvas.LeftProperty).ToBinding());
                        linePointsCollectionBinding.Bindings.Add(
                            pointPath.GetObservable(Canvas.TopProperty).ToBinding());
                    }
                }

                // Fill
                if (_multiDsChartItem.FillIsVisible && prevPointPath is not null)
                    ChildrenAddFillPolygon(genericContainer, pointPath, prevPointPath);

                prevPointPath = pointPath;
            }

            // Avalonia: PolyBezierSegment и PolyQuadraticBezierSegment отсутствуют.
            // Используем PolyLineSegment для Line, а Bezier/QuadraticBezier эмулируем через ListOfSegmentsBinding.
            if (_multiDsChartItem.LineIsVisible && linePathSegment is not null && linePointsCollectionBinding is not null)
            {
                if (linePathSegment is PolyLineSegment polyLine)
                {
                    polyLine.Bind(PolyLineSegment.PointsProperty, linePointsCollectionBinding);
                }
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

            // Avalonia: ToolTip через ToolTip.SetTip + обновление при PointerEntered
            ToolTip.SetTip(pointPath, "");
            pointPath.PointerEntered += (s, _) =>
            {
                if (s is Path p) ToolTip.SetTip(p, GetToolTip());
            };

            Children.Add(pointPath);

            pointPath.SetBindingOrConst(genericContainer, Shape.StrokeProperty, _multiDsChartItem.PointDsBrush,
                BindingMode.OneWay, UpdateSourceTrigger.Default, _visualDesignMode);

            pointPath.StrokeThickness = _multiDsChartItem.PointsIsVisible
                ? _multiDsChartItem.PointStrokeThickness
                : 0.0;

            // Canvas.Left binding
            BindingBase leftBinding;
            if (_multiDsChartItem.PointValueXInfo.IsConst)
            {
                double xMultiplier = pointsCount > 1 ? index / (double)(pointsCount - 1) : 0.5;

                // Avalonia: Bounds.Width вместо ActualWidth — используем перегрузку GetObservable со встроенным конвертером
                double multiplier = xMultiplier;
                leftBinding = this.GetObservable(BoundsProperty, b => (object?)(b.Width * multiplier))
                    .ToBinding();
            }
            else
            {
                var (leftMultiBinding, list) = StyledElementExtentions.CreateBindingWithoutConverter(
                    genericContainer, _multiDsChartItem.PointValueXInfo,
                    BindingMode.OneWay, UpdateSourceTrigger.Default);

                leftMultiBinding.Bindings.Add(new Binding
                {
                    Source = _multiChartDsShapeView.AxisXTopControl,
                    Path = nameof(TextTickBar.Maximum),
                    Mode = BindingMode.OneWay
                });
                leftMultiBinding.Bindings.Add(new Binding
                {
                    Source = _multiChartDsShapeView.AxisXTopControl,
                    Path = nameof(TextTickBar.Minimum),
                    Mode = BindingMode.OneWay
                });
                // Avalonia: Bounds вместо ActualWidth; конвертер получает Width
                leftMultiBinding.Bindings.Add(
                    this.GetObservable(BoundsProperty, b => (object?)b.Width).ToBinding());

                var converter = new LinearMultiValueConverter { Bias = 0, Gain = 1 };
                if (_multiDsChartItem.PointValueXInfo.IsConst || _visualDesignMode)
                    converter.ConstValue = _multiDsChartItem.PointValueXInfo.ConstValue;
                else
                    converter.ValueConverter =
                        _multiDsChartItem.PointValueXInfo.GetConverterOrDefaultConverter(genericContainer);
                leftMultiBinding.Converter = converter;
                leftBinding = leftMultiBinding;
            }

            // Avalonia: Canvas.Left — AttachedProperty, устанавливается через .Bind()
            pointPath.Bind(Canvas.LeftProperty, leftBinding);

            // Canvas.Top binding
            var (topMultiBinding, _) = StyledElementExtentions.CreateBindingWithoutConverter(
                genericContainer, _multiDsChartItem.PointValueYInfo,
                BindingMode.OneWay, UpdateSourceTrigger.Default);

            topMultiBinding.Bindings.Add(new Binding
            {
                Source = _multiChartDsShapeView.AxisYLeftControl,
                Path = nameof(TextTickBar.Maximum),
                Mode = BindingMode.OneWay
            });
            topMultiBinding.Bindings.Add(new Binding
            {
                Source = _multiChartDsShapeView.AxisYLeftControl,
                Path = nameof(TextTickBar.Minimum),
                Mode = BindingMode.OneWay
            });
            topMultiBinding.Bindings.Add(
                this.GetObservable(BoundsProperty, b => (object?)b.Height).ToBinding());

            var yConverter = new LinearMultiValueConverter { Bias = 1, Gain = -1 };
            if (_multiDsChartItem.PointValueYInfo.IsConst || _visualDesignMode)
                yConverter.ConstValue = _multiDsChartItem.PointValueYInfo.ConstValue;
            else
                yConverter.ValueConverter =
                    _multiDsChartItem.PointValueYInfo.GetConverterOrDefaultConverter(genericContainer);
            topMultiBinding.Converter = yConverter;

            pointPath.Bind(Canvas.TopProperty, topMultiBinding);

            return pointPath;
        }

        private void ChildrenAddLinePath(out PathFigure linePathFigure, out PathSegment? linePathSegment)
        {
            var dsShape = (MultiChartDsShape)_multiChartDsShapeView.DsShapeViewModel.DsShape;

            // Avalonia: PolyBezierSegment и PolyQuadraticBezierSegment отсутствуют.
            // Для всех типов используем PolyLineSegment (Bezier/QuadraticBezier в Avalonia не имеют Poly-вариантов).
            // TODO: для Bezier/QuadraticBezier потребуется отдельная реализация через коллекцию обычных сегментов.
            linePathSegment = _multiDsChartItem.Type switch
            {
                MultiDsChartItem.AppearanceType.Line => new PolyLineSegment(),
                MultiDsChartItem.AppearanceType.Bezier => new PolyLineSegment(),           // fallback
                MultiDsChartItem.AppearanceType.QuadraticBezier => new PolyLineSegment(),  // fallback
                _ => null
            };

            linePathFigure = new PathFigure();
            var linePathGeometry = new PathGeometry();
            var linePath = new Path();
            var toolTipLinePath = new Path();

            ToolTip.SetTip(toolTipLinePath, "");
            toolTipLinePath.PointerEntered += (s, _) =>
            {
                if (s is Path p) ToolTip.SetTip(p, GetToolTip());
            };

            // Avalonia: PathFigure.Segments — PathSegments (не nullable)
            if (linePathSegment is not null)
                linePathFigure.Segments!.Add(linePathSegment);

            // Avalonia: PathGeometry.Figures — PathFigures (не nullable)
            linePathGeometry.Figures!.Add(linePathFigure);
            linePath.Data = linePathGeometry;
            toolTipLinePath.Data = linePathGeometry;
            Children.Add(linePath);
            Children.Add(toolTipLinePath);

            linePath.SetBindingOrConst(dsShape.Container, Shape.StrokeProperty, _multiDsChartItem.LineDsBrush,
                BindingMode.OneWay, UpdateSourceTrigger.Default, _visualDesignMode);
            toolTipLinePath.Stroke = Brushes.Transparent;
            linePath.StrokeThickness = _multiDsChartItem.LineStrokeThickness;
            toolTipLinePath.StrokeThickness = _multiDsChartItem.LineStrokeThickness + 4;

            // Avalonia: StrokeDashArray — AvaloniaList<double>
            if (_multiDsChartItem.LineStrokeDashLength.HasValue && _multiDsChartItem.LineStrokeGapLength.HasValue)
            {
                linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
                    new AvaloniaList<double>
                    {
                        _multiDsChartItem.LineStrokeDashLength.Value,
                        _multiDsChartItem.LineStrokeGapLength.Value
                    });
            }
            else if (!string.IsNullOrEmpty(_multiDsChartItem.LineStrokeDashArray))
            {
                var parts = _multiDsChartItem.LineStrokeDashArray.Split(',');
                if (parts.Length > 1)
                    linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty,
                        new AvaloniaList<double>(
                            parts.Select(s => ObsoleteAnyHelper.ConvertTo<double>(s, false))));
                else
                    linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
            }
            else
            {
                linePath.SetConst(dsShape.Container, Shape.StrokeDashArrayProperty, null);
            }
        }

        private void ChildrenAddFillPolygon(GenericContainer genericContainer, Path pointPath, Path prevPointPath)
        {
            var polygonBinding = new MultiBinding
            {
                Mode = BindingMode.OneWay,
                Converter = new PolygonPointsCollectionValueConverter()
            };

            // Avalonia: GetObservable(Canvas.LeftProperty/TopProperty) вместо Binding { Path = PropertyPath(...) }
            polygonBinding.Bindings.Add(prevPointPath.GetObservable(Canvas.LeftProperty).ToBinding());
            polygonBinding.Bindings.Add(prevPointPath.GetObservable(Canvas.TopProperty).ToBinding());
            polygonBinding.Bindings.Add(pointPath.GetObservable(Canvas.LeftProperty).ToBinding());
            polygonBinding.Bindings.Add(pointPath.GetObservable(Canvas.TopProperty).ToBinding());
            polygonBinding.Bindings.Add(
                this.GetObservable(BoundsProperty, b => (object?)b.Height).ToBinding());

            var fillPolygon = new Polygon { StrokeThickness = 1 };
            fillPolygon.SetBindingOrConst(genericContainer, Shape.StrokeProperty, _multiDsChartItem.FillDsBrush,
                BindingMode.OneWay, UpdateSourceTrigger.Default, _visualDesignMode);
            fillPolygon.SetBindingOrConst(genericContainer, Shape.FillProperty, _multiDsChartItem.FillDsBrush,
                BindingMode.OneWay, UpdateSourceTrigger.Default, _visualDesignMode);
            Children.Add(fillPolygon);

            fillPolygon.ZIndex = -1;

            fillPolygon.Bind(Polygon.PointsProperty, polygonBinding);
        }

        private string GetToolTip()
        {
            var x = _multiChartDsShapeView.AxisXTopControl.Minimum +
                    (_multiChartDsShapeView.AxisXTopControl.Maximum -
                     _multiChartDsShapeView.AxisXTopControl.Minimum) * _lastPointerX / Bounds.Width;

            var y = _multiChartDsShapeView.AxisYLeftControl.Minimum +
                    (_multiChartDsShapeView.AxisYLeftControl.Maximum -
                     _multiChartDsShapeView.AxisYLeftControl.Minimum) *
                    (Bounds.Height - _lastPointerY) / Bounds.Height;

            return _multiChartDsShapeView.AxisXTopControl.GetValueWithEngUnit(x) +
                   CultureInfo.CurrentCulture.TextInfo.ListSeparator + " " +
                   _multiChartDsShapeView.AxisYLeftControl.GetValueWithEngUnit(y);
        }

        #endregion

        #region private fields

        private readonly MultiChartDsShapeView _multiChartDsShapeView;
        private readonly MultiDsChartItem _multiDsChartItem;
        private readonly bool _visualDesignMode;
        private double _lastPointerX;
        private double _lastPointerY;

        #endregion

        // ─── Внутренние конвертеры ────────────────────────────────────────────────

        // Avalonia IMultiValueConverter: IList<object?> вместо object?[]
        private class LinearMultiValueConverter : IMultiValueConverter, IDisposable
        {
            public bool Disposed { get; private set; }
            public ValueConverterBase? ValueConverter { get; set; }
            public double ConstValue { get; set; }
            public double Bias { get; set; }
            public double Gain { get; set; }

            public void Dispose()
            {
                if (Disposed) return;
                ValueConverter?.Dispose();
                ValueConverter = null;
                Disposed = true;
                GC.SuppressFinalize(this);
            }

            ~LinearMultiValueConverter() => Dispose();

            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                var length = values.Count;
                if (length < 3) return BindingOperations.DoNothing;

                double value;
                if (ValueConverter is null)
                {
                    value = ConstValue;
                }
                else
                {
                    var oValue = ValueConverter.Convert(values.ToArray(), typeof(double), parameter, culture);
                    if (oValue is not double d) return BindingOperations.DoNothing;
                    value = d;
                }

                var maximum = values[length - 3];
                var minimum = values[length - 2];
                var k = values[length - 1];
                if (maximum is not double dMax || minimum is not double dMin || k is not double dK)
                    return BindingOperations.DoNothing;

                var result = (Bias + Gain * (value - dMin) / (dMax - dMin)) * dK;
                if (double.IsNaN(result) || double.IsInfinity(result)) return BindingOperations.DoNothing;
                return result;
            }
        }

        private class PointValueConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values.Count < 2) return BindingOperations.DoNothing;
                if (values[0] is double x && values[1] is double y)
                    return new Point(x, y);
                return BindingOperations.DoNothing;
            }
        }

        // Avalonia: Polygon.Points — IList<Point>, тип `Points` отсутствует.
        private class PolygonPointsCollectionValueConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values.Count < 5) return BindingOperations.DoNothing;
                if (values[0] is double px && values[1] is double py &&
                    values[2] is double cx && values[3] is double cy &&
                    values[4] is double h)
                {
                    return new AvaloniaList<Point>
                    {
                        new Point(px, py),
                        new Point(cx, cy),
                        new Point(cx, h),
                        new Point(px, h)
                    };
                }
                return BindingOperations.DoNothing;
            }
        }

        // Avalonia: PolyLineSegment.Points — AvaloniaList<Point>?
        private class PointsCollectionValueConverter : IMultiValueConverter
        {
            public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
            {
                if (values.Count < 2) return BindingOperations.DoNothing;
                var pts = new AvaloniaList<Point>();
                for (var i = 0; i < values.Count / 2; i++)
                {
                    if (values[i * 2] is double x && values[i * 2 + 1] is double y)
                        pts.Add(new Point(x, y));
                }
                return pts;
            }
        }
    }
}
