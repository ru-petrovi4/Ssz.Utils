using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;



using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.ControlsDesign
{
    public partial class DesignDrawingCanvas : Canvas
    {
        #region internal functions

        internal async Task TryConvertUnderlyingContentXamlToDsShapesAsync()
        {
            var dsPageDrawing = DesignDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is null || _underlyingContentControl is null ||
                _underlyingContentControl.Content is null ||
                _underlyingContentControl.Content is Image)
                return;

            using (var busyCloser = DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                await busyCloser.SetHeaderAsync(Properties.Resources
                    .ProgressInfo_DescriptionLine1_TryConvertUnderlyingContentXamlToDsShapes);

                var extractedDsShapes = new List<DsShapeBase>();
                var succeeded = ExtractDsShapesFromUIElement(dsPageDrawing,
                    _underlyingContentControl.Content as UIElement, extractedDsShapes);
                if (succeeded)
                {
                    dsPageDrawing.UnderlyingXaml = new DsXaml();

                    if (extractedDsShapes.Count == 1)
                    {
                        dsPageDrawing.AddDsShapes(0, true, extractedDsShapes.ToArray());

                        foreach (DsShapeBase dsShape in extractedDsShapes) dsShape.RefreshForPropertyGrid();
                    }
                    else if (extractedDsShapes.Count > 1)
                    {
                        ComplexDsShape newComplexDsShape =
                            DsProject.Instance.NewComplexDsShape(extractedDsShapes.ToArray());

                        dsPageDrawing.AddDsShapes(0, true, newComplexDsShape);

                        newComplexDsShape.RefreshForPropertyGrid();
                    }
                }
            }
        }

        internal async Task TryConvertContentDsShapesToComplexDsShapesAsync()
        {
            var dsPageDrawing = DesignDrawingViewModel.Drawing as DsPageDrawing;
            if (dsPageDrawing is null) return;

            ContentDsShape[] contentDsShapes =
                dsPageDrawing.DsShapes
                    .Where(dsShape => dsShape.WidthInitial >= dsPageDrawing.Width / 2 &&
                                        dsShape.HeightInitial >= dsPageDrawing.Height / 2 &&
                                        dsShape is ContentDsShape)
                    .Select(dsShape => (ContentDsShape) dsShape)
                    .Where(dsShape => dsShape.ContentInfo.IsConst)
                    .ToArray();

            if (contentDsShapes.Length == 0) return;

            await DesignDrawingViewModel.TryConvertContentDsShapeToComplexDsShapeAsync(contentDsShapes);
        }

        #endregion

        #region private functions

        private bool ExtractDsShapesFromUIElement(DsPageDrawing dsPageDrawing, UIElement? uiElement,
            List<DsShapeBase> extractedDsShapes)
        {
            if (uiElement is null) return false;

            var path = uiElement as Path;
            if (path is not null)
            {
                var geometryDsShape = new GeometryDsShape();
                var succeeded = ProcessNewGeometryDsShape(dsPageDrawing, geometryDsShape, path);
                if (!succeeded) return false;

                extractedDsShapes.Add(geometryDsShape);
                return true;
            }

            var viewbox = uiElement as Viewbox;
            if (viewbox is not null)
            {
                if (viewbox.Child is null) return true;
                return ExtractDsShapesFromUIElement(dsPageDrawing, viewbox.Child, extractedDsShapes);
            }

            var FixedPage = uiElement as FixedPage;
            if (FixedPage is not null)
                return ExtractDsShapesFromUIElements(dsPageDrawing, FixedPage.Children.OfType<UIElement>(),
                    extractedDsShapes);

            var canvas = uiElement as Canvas;
            if (canvas is not null)
            {
                var extractedDsShapesFromCanvas = new List<DsShapeBase>();

                bool succeeded;

                if (canvas.Background is not null && canvas.Background != Brushes.Transparent)
                {
                    var geometryDsShape = new GeometryDsShape();
                    succeeded = ProcessNewDsShape(dsPageDrawing, geometryDsShape, canvas);
                    if (!succeeded) return false;
                    geometryDsShape.FillInfo.ConstValue = DsBrushBase.GetDsBrush(canvas.Background);
                    geometryDsShape.StrokeThickness = 0;
                    extractedDsShapesFromCanvas.Add(geometryDsShape);
                }

                succeeded = ExtractDsShapesFromUIElements(dsPageDrawing, canvas.Children.OfType<UIElement>(),
                    extractedDsShapesFromCanvas);
                if (!succeeded) return false;

                if (extractedDsShapesFromCanvas.Count == 1)
                {
                    extractedDsShapes.Add(extractedDsShapesFromCanvas.First());
                }
                else if (extractedDsShapesFromCanvas.Count > 1)
                {
                    ComplexDsShape newComplexDsShape =
                        DsProject.Instance.NewComplexDsShape(extractedDsShapesFromCanvas.ToArray());

                    extractedDsShapes.Add(newComplexDsShape);
                }

                return true;
            }

            var fe = uiElement as FrameworkElement;
            if (fe is not null)
            {
                var contentDsShape = new ContentDsShape();
                var succeeded = ProcessNewDsShape(dsPageDrawing, contentDsShape, fe);
                if (!succeeded) return false;

                fe = XamlHelper.Load(XamlHelper.Save(fe)) as FrameworkElement;

                var viewBox = new Viewbox {Child = fe, Stretch = Stretch.Fill};

                contentDsShape.ContentInfo.ConstValue.Xaml = XamlHelper.Save(viewBox);

                extractedDsShapes.Add(contentDsShape);
                return true;
            }

            return false;
        }


        private bool ExtractDsShapesFromUIElements(DsPageDrawing dsPageDrawing,
            IEnumerable<UIElement> uiElements, List<DsShapeBase> extractedDsShapes)
        {
            foreach (UIElement uiElement in uiElements)
            {
                var succeeded = ExtractDsShapesFromUIElement(dsPageDrawing, uiElement, extractedDsShapes);
                if (!succeeded) return false;
            }

            return true;
        }


        private bool ProcessNewDsShape(DsPageDrawing dsPageDrawing, DsShapeBase dsShape,
            FrameworkElement fe)
        {
            if (double.IsNaN(fe.ActualWidth) || double.IsInfinity(fe.ActualWidth) ||
                double.IsNaN(fe.ActualHeight) || double.IsInfinity(fe.ActualHeight)) return false;
            /*
            Rect rect = fe.TransformToVisual(canvas)
                .TransformBounds(LayoutInformation.GetLayoutSlot(fe));

            dsShape.WidthInitial = rect.Width;
            dsShape.HeightInitial = rect.Height;
            dsShape.LeftNotTransformed = rect.X;
            dsShape.TopNotTransformed = rect.Y;*/

            var p1 = fe.TranslatePoint(new Point(0, 0), this);
            var p2 = fe.TranslatePoint(new Point(fe.ActualWidth, fe.ActualHeight), this);
            var width = Math.Abs(p2.X - p1.X);
            if (width == 0.0) width = dsPageDrawing.Width;
            dsShape.WidthInitial = width;
            var height = Math.Abs(p2.Y - p1.Y);
            if (height == 0.0) height = dsPageDrawing.Height;
            dsShape.HeightInitial = height;
            dsShape.LeftNotTransformed = Math.Min(p1.X, p2.X);
            dsShape.TopNotTransformed = Math.Min(p1.Y, p2.Y);

            if (dsShape.WidthInitial > dsPageDrawing.Width / 2 &&
                dsShape.HeightInitial > dsPageDrawing.Height / 2) dsShape.IsLocked = true;

            dsShape.Name = dsShape.GetDsShapeTypeNameToDisplay();

            return true;
        }

        private bool ProcessNewGeometryDsShape(DsPageDrawing dsPageDrawing, GeometryDsShape dsShape,
            Path path)
        {
            var pathGeometry = path.Data as PathGeometry;
            if (pathGeometry is null) pathGeometry = PathGeometry.CreateFromGeometry(path.Data);
            if (pathGeometry is null) return false;
            if (path.Fill is not null && path.Fill != Brushes.Transparent)
                foreach (var f in pathGeometry.Figures)
                    if (!f.IsFilled)
                        return false;

            dsShape.FillInfo.ConstValue = DsBrushBase.GetDsBrush(path.Fill);
            dsShape.StrokeInfo.ConstValue = DsBrushBase.GetDsBrush(path.Stroke);
            if (path.StrokeDashArray.Count > 0)
                dsShape.StrokeDashArray = string.Join(@",",
                    path.StrokeDashArray.Select(d => ObsoleteAnyHelper.ConvertTo<string>(d, false)));
            dsShape.StrokeLineJoin = path.StrokeLineJoin;
            dsShape.StrokeStartLineCap = path.StrokeStartLineCap;
            dsShape.StrokeEndLineCap = path.StrokeEndLineCap;

            if (path.Stretch == Stretch.Fill && (path.RenderTransform is null || path.RenderTransform.Value.IsIdentity))
            {
                var succeeded = ProcessNewDsShape(dsPageDrawing, dsShape, path);
                if (!succeeded) return false;

                dsShape.GeometryInfo.TypeString = DsUIElementPropertySupplier.CustomTypeString;
                dsShape.GeometryInfo.CustomXamlString = pathGeometry.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                pathGeometry.DoForAllPoints((ref Point p) => p = path.TranslatePoint(p, this));

                //var bounds = pathGeometry.GetRenderBounds(
                //            new Pen(path.Stroke, path.StrokeThickness));
                var bounds = pathGeometry.Bounds;
                if (double.IsNaN(bounds.X) || double.IsInfinity(bounds.X) ||
                    double.IsNaN(bounds.Y) || double.IsInfinity(bounds.Y) ||
                    double.IsNaN(bounds.Width) || double.IsInfinity(bounds.Width) ||
                    double.IsNaN(bounds.Height) || double.IsInfinity(bounds.Height)) return false;

                double strokeThickness;
                if (path.Stretch == Stretch.None)
                {
                    strokeThickness = path.StrokeThickness;
                }
                else
                {
                    if (!double.IsNaN(path.Width) && !double.IsNaN(path.Height))
                    {
                        var kX = path.Width / bounds.Width;
                        var kY = path.Height / bounds.Height;
                        var k = Math.Min(kX, kY);
                        strokeThickness = Math.Round(path.StrokeThickness / k, 1);
                    }
                    else
                    {
                        strokeThickness = path.StrokeThickness;
                    }
                }

                var width = bounds.Width + strokeThickness;
                if (width == 0.0) width = DsShapeBase.MinWidth;
                dsShape.WidthInitial = width;
                var height = bounds.Height + strokeThickness;
                if (height == 0.0) height = DsShapeBase.MinHeight;
                dsShape.HeightInitial = height;
                var left = bounds.X - strokeThickness / 2;
                var top = bounds.Y - strokeThickness / 2;
                dsShape.LeftNotTransformed = left;
                dsShape.TopNotTransformed = top;

                if (dsShape.WidthInitial > dsPageDrawing.Width / 2 &&
                    dsShape.HeightInitial > dsPageDrawing.Height / 2) dsShape.IsLocked = true;

                dsShape.Name = dsShape.GetDsShapeTypeNameToDisplay();

                pathGeometry.DoForAllPoints((ref Point p) =>
                {
                    p.X = p.X - bounds.X;
                    p.Y = p.Y - bounds.Y;
                });

                dsShape.GeometryInfo = new DsUIElementProperty
                {
                    TypeString = DsUIElementPropertySupplier.CustomTypeString,
                    CustomXamlString = pathGeometry.ToString(CultureInfo.InvariantCulture)
                };
            }

            if (path.Stretch == Stretch.None)
            {
                dsShape.StrokeThickness = path.StrokeThickness;
            }
            else
            {
                if (!double.IsNaN(path.Width) && !double.IsNaN(path.Height))
                {
                    var kX = path.Width / dsShape.WidthInitialNotRounded;
                    var kY = path.Height / dsShape.HeightInitialNotRounded;
                    var k = Math.Min(kX, kY);
                    dsShape.StrokeThickness = Math.Round(path.StrokeThickness / k, 1);
                }
                else
                {
                    dsShape.StrokeThickness = path.StrokeThickness;
                }
            }

            return true;
        }

        #endregion
    }
}


//if (succeeded)
//{
//    extractedDsShapes.Add(geometryDsShape);
//    return true;
//}
//else
//{
//    var contentDsShape = new ContentDsShape();
//    succeeded = ProcessNewDsShape(dsPageDrawing, contentDsShape, path);
//    if (!succeeded) return false;
//    var newCanvas = new Canvas()
//    {
//        Width = path.ActualWidth,
//        Height = path.ActualHeight,
//    };
//    newCanvas.Children.Add((UIElement)XamlHelper.Load(XamlHelper.Save(path)));
//    var newViewBox = new Viewbox { Child = newCanvas, Stretch = Stretch.Fill };
//    contentDsShape.ContentInfo.ConstValue.Xaml = XamlHelper.Save(newViewBox);

//    extractedDsShapes.Add(contentDsShape);
//    return true;
//}