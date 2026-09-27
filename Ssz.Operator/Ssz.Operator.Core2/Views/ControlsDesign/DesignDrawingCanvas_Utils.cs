using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     Turns the XAML a page carries underneath its shapes into shapes the author can edit.
    ///     <para>
    ///         Ported from the WPF editor without two of its branches: an XPS FixedPage and a
    ///         SharpVectors SvgViewbox, neither of which exists outside WPF. Content of those kinds is
    ///         left as it is, which is what the WPF version did with anything else it could not read.
    ///     </para>
    /// </summary>
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

            using (DesignDsProjectViewModel.BusyCloser busyCloser =
                   DesignDsProjectViewModel.Instance.GetBusyCloser())
            {
                await busyCloser.SetHeaderAsync(Properties.Resources
                    .ProgressInfo_DescriptionLine1_TryConvertUnderlyingContentXamlToDsShapes);

                var extractedDsShapes = new List<DsShapeBase>();
                var succeeded = ExtractDsShapesFromControl(dsPageDrawing,
                    _underlyingContentControl.Content as Control, extractedDsShapes);
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

        private bool ExtractDsShapesFromControl(DsPageDrawing dsPageDrawing, Control? control,
            List<DsShapeBase> extractedDsShapes)
        {
            if (control is null || control is Image)
                return false;

            var path = control as Path;
            if (path is not null)
            {
                var geometryDsShape = new GeometryDsShape();
                var succeeded = ProcessNewGeometryDsShape(dsPageDrawing, geometryDsShape, path);
                if (!succeeded) return false;

                extractedDsShapes.Add(geometryDsShape);
                return true;
            }

            var viewbox = control as Viewbox;
            if (viewbox is not null)
            {
                if (viewbox.Child is null) return true;
                return ExtractDsShapesFromControl(dsPageDrawing, viewbox.Child as Control, extractedDsShapes);
            }

            var canvas = control as Canvas;
            if (canvas is not null)
            {
                var extractedDsShapesFromCanvas = new List<DsShapeBase>();

                bool succeeded;

                if (canvas.Background is not null && !Equals(canvas.Background, Brushes.Transparent))
                {
                    var geometryDsShape = new GeometryDsShape();
                    succeeded = ProcessNewDsShape(dsPageDrawing, geometryDsShape, canvas);
                    if (!succeeded) return false;
                    geometryDsShape.FillInfo.ConstValue = DsBrushBase.GetDsBrush(canvas.Background as Brush);
                    geometryDsShape.StrokeThickness = 0;
                    extractedDsShapesFromCanvas.Add(geometryDsShape);
                }

                succeeded = ExtractDsShapesFromControls(dsPageDrawing, canvas.Children.OfType<Control>(),
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

            var contentDsShape = new ContentDsShape();
            var contentSucceeded = ProcessNewDsShape(dsPageDrawing, contentDsShape, control);
            if (!contentSucceeded) return false;

            // The control is re-created from its own markup, because one control cannot be in two
            // visual trees and this one is still in the page.
            var reloaded = XamlHelper.Load(XamlHelper.Save(control)) as Control;

            var newViewbox = new Viewbox { Child = reloaded, Stretch = Stretch.Fill };

            contentDsShape.ContentInfo.ConstValue.Xaml = XamlHelper.Save(newViewbox);

            extractedDsShapes.Add(contentDsShape);
            return true;
        }

        private bool ExtractDsShapesFromControls(DsPageDrawing dsPageDrawing,
            IEnumerable<Control> controls, List<DsShapeBase> extractedDsShapes)
        {
            foreach (Control control in controls)
            {
                var succeeded = ExtractDsShapesFromControl(dsPageDrawing, control, extractedDsShapes);
                if (!succeeded) return false;
            }

            return true;
        }

        private bool ProcessNewDsShape(DsPageDrawing dsPageDrawing, DsShapeBase dsShape, Control control)
        {
            Rect bounds = control.Bounds;
            if (Double.IsNaN(bounds.Width) || Double.IsInfinity(bounds.Width) ||
                Double.IsNaN(bounds.Height) || Double.IsInfinity(bounds.Height)) return false;

            Point? p1 = control.TranslatePoint(new Point(0, 0), this);
            Point? p2 = control.TranslatePoint(new Point(bounds.Width, bounds.Height), this);
            if (p1 is null || p2 is null) return false;

            var width = Math.Abs(p2.Value.X - p1.Value.X);
            if (width == 0.0) width = dsPageDrawing.Width;
            dsShape.WidthInitial = width;
            var height = Math.Abs(p2.Value.Y - p1.Value.Y);
            if (height == 0.0) height = dsPageDrawing.Height;
            dsShape.HeightInitial = height;
            dsShape.LeftNotTransformed = Math.Min(p1.Value.X, p2.Value.X);
            dsShape.TopNotTransformed = Math.Min(p1.Value.Y, p2.Value.Y);

            if (dsShape.WidthInitial > dsPageDrawing.Width / 2 &&
                dsShape.HeightInitial > dsPageDrawing.Height / 2) dsShape.IsLocked = true;

            dsShape.Name = dsShape.GetDsShapeTypeNameToDisplay();

            return true;
        }

        private bool ProcessNewGeometryDsShape(DsPageDrawing dsPageDrawing, GeometryDsShape dsShape, Path path)
        {
            PathGeometry? pathGeometry = GetPathGeometry(path.Data);
            if (pathGeometry is null) return false;
            if (path.Fill is not null && !Equals(path.Fill, Brushes.Transparent))
                foreach (PathFigure f in pathGeometry.Figures ?? Enumerable.Empty<PathFigure>())
                    if (!f.IsFilled)
                        return false;

            dsShape.FillInfo.ConstValue = DsBrushBase.GetDsBrush(path.Fill as Brush);
            dsShape.StrokeInfo.ConstValue = DsBrushBase.GetDsBrush(path.Stroke as Brush);
            if (path.StrokeDashArray is not null && path.StrokeDashArray.Count > 0)
                dsShape.StrokeDashArray = String.Join(@",",
                    path.StrokeDashArray.Select(d => ObsoleteAnyHelper.ConvertTo<string>(d, false)));
            dsShape.StrokeLineJoin = path.StrokeJoin;
            dsShape.StrokeStartLineCap = path.StrokeLineCap;

            if (path.Stretch == Stretch.Fill &&
                (path.RenderTransform is null || path.RenderTransform.Value.IsIdentity))
            {
                var succeeded = ProcessNewDsShape(dsPageDrawing, dsShape, path);
                if (!succeeded) return false;

                dsShape.GeometryInfo.TypeString = DsUIElementPropertySupplier.CustomTypeString;
                dsShape.GeometryInfo.CustomXamlString = XamlHelper.Save(pathGeometry);
            }
            else
            {
                pathGeometry.DoForAllPoints((ref Point p) =>
                {
                    Point? translated = path.TranslatePoint(p, this);
                    if (translated is not null) p = translated.Value;
                });

                Rect bounds = pathGeometry.Bounds;
                if (Double.IsNaN(bounds.X) || Double.IsInfinity(bounds.X) ||
                    Double.IsNaN(bounds.Y) || Double.IsInfinity(bounds.Y) ||
                    Double.IsNaN(bounds.Width) || Double.IsInfinity(bounds.Width) ||
                    Double.IsNaN(bounds.Height) || Double.IsInfinity(bounds.Height)) return false;

                double strokeThickness;
                if (path.Stretch == Stretch.None)
                {
                    strokeThickness = path.StrokeThickness;
                }
                else
                {
                    if (!Double.IsNaN(path.Width) && !Double.IsNaN(path.Height))
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
                dsShape.LeftNotTransformed = bounds.X - strokeThickness / 2;
                dsShape.TopNotTransformed = bounds.Y - strokeThickness / 2;

                if (dsShape.WidthInitial > dsPageDrawing.Width / 2 &&
                    dsShape.HeightInitial > dsPageDrawing.Height / 2) dsShape.IsLocked = true;

                dsShape.Name = dsShape.GetDsShapeTypeNameToDisplay();

                pathGeometry.DoForAllPoints((ref Point p) => { p = new Point(p.X - bounds.X, p.Y - bounds.Y); });

                dsShape.GeometryInfo = new DsUIElementProperty
                {
                    TypeString = DsUIElementPropertySupplier.CustomTypeString,
                    CustomXamlString = XamlHelper.Save(pathGeometry)
                };
            }

            if (path.Stretch == Stretch.None)
            {
                dsShape.StrokeThickness = path.StrokeThickness;
            }
            else
            {
                if (!Double.IsNaN(path.Width) && !Double.IsNaN(path.Height))
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

        /// <summary>
        ///     WPF could turn any geometry into an editable path through PathGeometry.CreateFromGeometry.
        ///     Avalonia has no such conversion, so a geometry that is not already a path is re-read from
        ///     its own markup, and one that cannot be is left alone.
        /// </summary>
        private PathGeometry? GetPathGeometry(Geometry? geometry)
        {
            if (geometry is null) return null;

            var pathGeometry = geometry as PathGeometry;
            if (pathGeometry is not null) return pathGeometry;

            try
            {
                return PathGeometry.Parse(geometry.ToString() ?? @"");
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}
