using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using d = System.Drawing;
using d2 = System.Drawing.Drawing2D;
using di = System.Drawing.Imaging;

namespace Ssz.Utils.Wpf
{
    public static class Xaml2Emf
    {
        #region public functions  
        
        public static ILogger? Logger { get; set; }

        public static void RealizeFrameworkElement(FrameworkElement fe)
        {
            var size = new Size(double.MaxValue, double.MaxValue);
            if (fe.Width > 0 && fe.Height > 0) size = new Size(fe.Width, fe.Height);
            fe.Measure(size);
            fe.Arrange(new Rect(new Point(), fe.DesiredSize));
            fe.UpdateLayout();
        }

        public static Drawing? GetDrawingFromObj(object? obj)
        {
            if (obj is null) return null;

            Drawing? drawing = FindDrawing(obj);
            if (drawing is not null) return drawing;

            var fe = obj as FrameworkElement;
            if (fe is not null)
            {
                RealizeFrameworkElement(fe);
                drawing = WalkVisual(fe);
            }

            // Handle FrameworkContentElement

            return drawing;
        }

        public static void MakeDrawingSerializable(Drawing drawing)
        {
            InternalMakeDrawingSerializable(drawing, new GeometryValueSerializer());
        }

        public static void RenderDrawingToGraphics(Drawing drawing, d.Graphics graphics)
        {
            SetGraphicsQuality(graphics);
            drawing.RenderTo(graphics, 1);
        }

        /// <summary>
        ///     drawing is not null
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="drawing"></param>
        public static void CreateEmf(string fileName, Drawing drawing)
        {
            if (drawing is null) throw new ArgumentNullException(@"drawing");

            Rect bounds = drawing.Bounds;
            if (bounds.Width == 0 || bounds.Height == 0) bounds = new Rect(0, 0, 1, 1);
            using (d.Graphics refDC = d.Graphics.FromImage(new d.Bitmap(1, 1)))
            using (FileStream fileStream = File.Create(fileName))
            {
                using (var g = d.Graphics.FromImage(new di.Metafile(fileStream, refDC.GetHdc(),
                    bounds.ToGdiPlus(), di.MetafileFrameUnit.Pixel, di.EmfType.EmfPlusDual)))
                {
                    RenderDrawingToGraphics(drawing, g);
                }
            }                
        }

        public static void SetGraphicsQuality(d.Graphics graphics)
        {
            graphics.SmoothingMode = d2.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = d2.InterpolationMode.HighQualityBicubic;
        }

        #endregion

        #region internal functions

        internal static bool IsZero(double d)
        {
            return Math.Abs(d) < 2e-05;
        }

        #endregion

        #region private functions

        private static void InternalMakeDrawingSerializable(Drawing drawing, GeometryValueSerializer gvs)
        {
            var dg = drawing as DrawingGroup;
            if (dg is not null)
                for (int i = 0; i < dg.Children.Count; ++i)
                    InternalMakeDrawingSerializable(dg.Children[i], gvs);
            else
            {
                var gd = drawing as GeometryDrawing;
                if (gd is not null)
                {
                    var sg = gd.Geometry as StreamGeometry;
                    if (sg is not null && !gvs.CanConvertToString(sg, null))
                        gd.Geometry = PathGeometry.CreateFromGeometry(sg);
                }
            }
        }

        private static Drawing? FindDrawing(object obj)
        {
            var drawing = obj as Drawing;
            if (drawing is not null) return drawing;
            var db = obj as DrawingBrush;
            if (db is not null) return db.Drawing;
            var di = obj as DrawingImage;
            if (di is not null) return di.Drawing;
            var dv = obj as DrawingVisual;
            if (dv is not null) return dv.Drawing;
            var rd = obj as ResourceDictionary;
            if (rd is not null)
            {
                foreach (object v in rd.Values)
                {
                    Drawing? d = FindDrawing(v);
                    if (d is not null)
                        if (drawing is null)
                            drawing = d;
                        else
                            throw new ArgumentException(
                                "Multiple Drawings found in ResourceDictionary", "xaml");
                }
                if (drawing is not null) return drawing;
            }
            return null;
        }

        private static DrawingGroup WalkVisual(Visual visual)
        {
            DrawingGroup vd = VisualTreeHelper.GetDrawing(visual);
            BitmapEffect be = VisualTreeHelper.GetBitmapEffect(visual);
            BitmapEffectInput bei = VisualTreeHelper.GetBitmapEffectInput(visual);
            Geometry cg = VisualTreeHelper.GetClip(visual);
            double op = VisualTreeHelper.GetOpacity(visual);
            Brush om = VisualTreeHelper.GetOpacityMask(visual);
            GuidelineSet? gs = GetGuidelines(visual);
            Transform? tx = GetTransform(visual);

            DrawingGroup? dg = null;
            if (be is null && cg is null && om is null && gs is null && tx is null && IsZero(op - 1))
            {
                dg = vd ?? new DrawingGroup();
            }
            else
            {
                dg = new DrawingGroup();
                if (be is not null) dg.BitmapEffect = be;
                if (bei is not null) dg.BitmapEffectInput = bei;
                if (cg is not null) dg.ClipGeometry = cg;
                if (!IsZero(op - 1)) dg.Opacity = op;
                if (om is not null) dg.OpacityMask = om;
                if (gs is not null) dg.GuidelineSet = gs;
                if (tx is not null) dg.Transform = tx;
                if (vd is not null) dg.Children.Add(vd);
            }

            int c = VisualTreeHelper.GetChildrenCount(visual);
            for (int i = 0; i < c; ++i) dg.Children.Add(WalkVisual(GetChild(visual, i)));
            return dg;
        }

        private static GuidelineSet? GetGuidelines(Visual visual)
        {
            DoubleCollection gx = VisualTreeHelper.GetXSnappingGuidelines(visual);
            DoubleCollection gy = VisualTreeHelper.GetYSnappingGuidelines(visual);
            if (gx is null && gy is null) return null;
            var gs = new GuidelineSet();
            if (gx is not null) gs.GuidelinesX = gx;
            if (gy is not null) gs.GuidelinesY = gy;
            return gs;
        }

        private static Transform? GetTransform(Visual visual)
        {
            Transform t = VisualTreeHelper.GetTransform(visual);
            Vector o = VisualTreeHelper.GetOffset(visual);

            if (IsZero(o.X) && IsZero(o.Y))
            {
                if (!IsIdentity(t)) return t;
            }
            else if (IsIdentity(t))
            {
                return new TranslateTransform(o.X, o.Y);
            }
            else
            {
                Matrix m = t.Value;
                m.Translate(o.X, o.Y);
                return new MatrixTransform(m);
            }
            return null;
        }

        private static bool IsIdentity(Transform t)
        {
            return t is null || t.Value.IsIdentity;
        }

        private static Visual GetChild(Visual visual, int index)
        {
            DependencyObject o = VisualTreeHelper.GetChild(visual, index);
            var v = o as Visual;
            if (v is null) throw new NotImplementedException("Visual3D not implemented");
            return v;
        }

        #endregion
    }

    internal static class DrawingExtensions
    {
        #region public functions

        public static void RenderTo(this Drawing drawing, d.Graphics graphics, double opacity)
        {            
            if (drawing is GeometryDrawing gd)
                gd.RenderTo(graphics, opacity);
            else if (drawing is GlyphRunDrawing grd)
                grd.RenderTo(graphics, opacity);
            else if (drawing is ImageDrawing id)
                id.RenderTo(graphics, opacity);
            else if (drawing is DrawingGroup dg)
                dg.RenderTo(graphics, opacity);
            else if (drawing is VideoDrawing vd)
                vd.RenderTo(graphics, opacity);
            else
                throw new ArgumentOutOfRangeException("drawing", drawing.GetType().ToString());
        }

        #endregion

        #region private functions

        private static void RenderTo(this DrawingGroup drawing, d.Graphics graphics, double opacity)
        {
            d2.GraphicsContainer gc = graphics.BeginContainer();
            Xaml2Emf.SetGraphicsQuality(graphics);
            if (drawing.Transform is not null && !drawing.Transform.Value.IsIdentity)
                graphics.MultiplyTransform(drawing.Transform.Value.ToGdiPlus(), d2.MatrixOrder.Prepend);
            if (drawing.ClipGeometry is not null)
                graphics.Clip = new d.Region(drawing.ClipGeometry.ToGdiPlus());
            if (!Xaml2Emf.IsZero(drawing.Opacity - 1) && drawing.Children.Count > 1)
            {
                bool intersects = false;
                int c = drawing.Children.Count;
                var b = new Rect[c];
                for (int i = 0; i < c; ++i) b[i] = drawing.Children[i].Bounds;
                for (int i = 0; i < c; ++i)
                {
                    for (int j = 0; j < c; ++j)
                        if (i != j && Rect.Intersect(b[i], b[j]) != Rect.Empty)
                        {
                            intersects = true;
                            break;
                        }
                    if (intersects) break;
                }
                if (intersects)
                    Xaml2Emf.Logger?.LogWarning("DrawingGroup.Opacity creates translucency between overlapping children");
            }
            foreach (Drawing d in drawing.Children) d.RenderTo(graphics, opacity*drawing.Opacity);
            graphics.EndContainer(gc);
            if (drawing.OpacityMask is not null) Xaml2Emf.Logger?.LogWarning("DrawingGroup OpacityMask ignored.");
            if (drawing.BitmapEffect is not null) Xaml2Emf.Logger?.LogWarning("DrawingGroup BitmapEffect ignored.");
            if (drawing.GuidelineSet is not null) Xaml2Emf.Logger?.LogWarning("DrawingGroup GuidelineSet ignored.");
        }

        private static void RenderTo(this GeometryDrawing drawing, d.Graphics graphics, double opacity)
        {
            if (drawing.Geometry is null || drawing.Geometry.IsEmpty()) return;
            var path = drawing.Geometry.ToGdiPlus();
            Brush brush = drawing.Brush;
            if (brush is not null)
            {
                if (!Xaml2Emf.IsZero(opacity - 1))
                {
                    brush = brush.Clone();
                    brush.Opacity *= opacity;
                }
                graphics.FillPath(brush.ToGdiPlus(drawing.Geometry.Bounds), path);
            }
            Pen pen = drawing.Pen;
            if (pen is not null)
            {
                if (!Xaml2Emf.IsZero(opacity - 1))
                {
                    pen = pen.Clone();
                    pen.Brush.Opacity *= opacity;
                }
                graphics.DrawPath(pen.ToGdiPlus(drawing.Geometry.GetRenderBounds(pen)), path);
            }
        }

        private static void RenderTo(this GlyphRunDrawing drawing, d.Graphics graphics, double opacity)
        {
            Geometry geo = drawing.GlyphRun.BuildGeometry();
            Brush brush = drawing.ForegroundBrush;
            if (geo is not null && brush is not null)
            {
                if (!Xaml2Emf.IsZero(opacity - 1))
                {
                    brush = brush.Clone();
                    brush.Opacity *= opacity;
                }
                graphics.FillPath(brush.ToGdiPlus(geo.Bounds), geo.ToGdiPlus());
            }
        }

        private static void RenderTo(this ImageDrawing drawing, d.Graphics graphics, double opacity)
        {
            d.Image? image = drawing.ImageSource.ToGdiPlus();
            if (image is not null)
            {
                var ia = new di.ImageAttributes();
                ia.SetColorMatrix(new di.ColorMatrix {Matrix33 = (float) opacity});
                Rect r = drawing.Rect;
                graphics.DrawImage(image,
                    new[] {r.TopLeft.ToGdiPlus(), r.TopRight.ToGdiPlus(), r.BottomLeft.ToGdiPlus()},
                    new d.RectangleF(0, 0, image.Width, image.Height), d.GraphicsUnit.Pixel, ia);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "opacity")]
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "graphics")]
        private static void RenderTo(this VideoDrawing drawing, d.Graphics graphics, double opacity)
        {
            Xaml2Emf.Logger?.LogWarning("Ignoring Video at {0}", drawing.Bounds);
        }

        #endregion
    }

    internal static class PenExtensions
    {
        #region public functions

        public static d.Pen ToGdiPlus(this Pen pen, Rect bounds)
        {
            var scb = pen.Brush as SolidColorBrush;
            d.Pen p;
            if (scb is not null)
                p = new d.Pen(scb.Color.ToGdiPlus(scb.Opacity), (float) pen.Thickness);
            else
                p = new d.Pen(pen.Brush.ToGdiPlus(bounds), (float) pen.Thickness);
            p.LineJoin = pen.LineJoin.ToGdiPlus();
            p.MiterLimit = (float) pen.MiterLimit;
            p.StartCap = pen.StartLineCap.ToGdiPlus();
            p.EndCap = pen.EndLineCap.ToGdiPlus();
            DashStyle ds = pen.DashStyle;
            if (ds != DashStyles.Solid)
            {
                var pattern = new List<float>();
                int fudge = pen.DashCap != PenLineCap.Flat ? -1 : 0;
                for (int i = 0; i < (ds.Dashes.Count%2) + 1; ++i)
                    foreach (double dash in ds.Dashes) pattern.Add((float) dash + (fudge *= -1));

                bool dashstart = true;
                int j = 0;
                while (j < pattern.Count)
                {
                    if (pattern[j] == 0)
                    {
                        pattern.RemoveAt(j);
                        if (j == 0)
                            dashstart = !dashstart;
                        else if (j > pattern.Count - 1)
                            break;
                        else
                        {
                            pattern[j - 1] += pattern[j];
                            pattern.RemoveAt(j);
                        }
                    }
                    else
                        j++;
                }

                if (pattern.Count < 2)
                    if (dashstart)
                        return p;
                    else
                        return new d.Pen(d.Color.FromArgb(0, 0, 0, 0), (float) pen.Thickness);

                if (!dashstart)
                {
                    float first = pattern[0];
                    pattern.RemoveAt(0);
                    pattern.Add(first);
                    ds.Offset -= first;
                }
                if (pattern.Min() <= 0) return p; // TODO
                p.DashPattern = pattern.ToArray();
                p.DashOffset = (float) ds.Offset;
                if (pen.DashCap == PenLineCap.Square) p.DashOffset += 0.5f;
                p.DashCap = pen.DashCap.ToDashCap();
            }
            return p;
        }

        public static d2.LineJoin ToGdiPlus(this PenLineJoin me)
        {
            switch (me)
            {
                case PenLineJoin.Bevel:
                    return d2.LineJoin.Bevel;
                case PenLineJoin.Round:
                    return d2.LineJoin.Round;
            }
            return d2.LineJoin.Miter;
        }

        public static d2.LineCap ToGdiPlus(this PenLineCap me)
        {
            switch (me)
            {
                case PenLineCap.Square:
                    return d2.LineCap.Square;
                case PenLineCap.Round:
                    return d2.LineCap.Round;
                case PenLineCap.Triangle:
                    return d2.LineCap.Triangle;
            }
            return d2.LineCap.Flat;
        }

        public static d2.DashCap ToDashCap(this PenLineCap me)
        {
            switch (me)
            {
                case PenLineCap.Round:
                    return d2.DashCap.Round;
                case PenLineCap.Triangle:
                    return d2.DashCap.Triangle;
            }
            return d2.DashCap.Flat;
        }

        #endregion
    }

    internal static class BrushExtensions
    {
        #region public functions

        public static d.Brush ToGdiPlus(this Brush brush, Rect bounds)
        {            

            d.Brush b;
            if (brush is SolidColorBrush sc)
                b = sc.ToGdiPlus(bounds);
            else if (brush is LinearGradientBrush lg)
                b = lg.ToGdiPlus(bounds);
            else if (brush is RadialGradientBrush rg)
                b = rg.ToGdiPlus(bounds);
            else if (brush is ImageBrush i)
                b = i.ToGdiPlus(bounds);
            else if (brush is DrawingBrush d)
                b = d.ToGdiPlus(bounds);
            else if (brush is VisualBrush v)
                b = v.ToGdiPlus(bounds);
            else
                throw new ArgumentOutOfRangeException("brush", brush.GetType().ToString());
            return b;
        }

        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "bounds")]
        public static d.Brush ToGdiPlus(this SolidColorBrush brush, Rect bounds)
        {
            return new d.SolidBrush(brush.Color.ToGdiPlus(brush.Opacity));
        }

        public static d.Brush ToGdiPlus(this DrawingBrush brush, Rect bounds)
        {
            Xaml2Emf.Logger?.LogWarning("Ignoring {0} at {1}", brush.GetType(), bounds);
            return new d.SolidBrush(d.Color.FromArgb(0, 255, 255, 255));
        }

        public static d.Brush ToGdiPlus(this VisualBrush brush, Rect bounds)
        {
            Xaml2Emf.Logger?.LogWarning("Ignoring {0} at {1}", brush.GetType(), bounds);
            return new d.SolidBrush(d.Color.FromArgb(0, 255, 255, 255));
        }

        public static d.Color ToGdiPlus(this Color color, double opacity)
        {
            return d.Color.FromArgb((int) Math.Round(opacity*color.A), color.R, color.G, color.B);
        }

        public static d2.WrapMode ToGdiPlus(this GradientSpreadMethod me)
        {
            switch (me)
            {
                case GradientSpreadMethod.Reflect:
                    return d2.WrapMode.TileFlipXY;
                case GradientSpreadMethod.Repeat:
                    return d2.WrapMode.Tile;
            }
            return d2.WrapMode.Clamp;
        }

        public static d2.WrapMode ToGdiPlus(this TileMode me)
        {
            switch (me)
            {
                case TileMode.Tile:
                    return d2.WrapMode.Tile;
                case TileMode.FlipX:
                    return d2.WrapMode.TileFlipX;
                case TileMode.FlipY:
                    return d2.WrapMode.TileFlipY;
                case TileMode.FlipXY:
                    return d2.WrapMode.TileFlipXY;
            }
            return d2.WrapMode.Clamp;
        }

        public static d.Image? ToGdiPlus(this ImageSource me)
        {
            Uri? url;
            if (Uri.TryCreate(me.ToString(), UriKind.Absolute, out url))
                if (url.IsFile)
                    try
                    {
                        return d.Image.FromFile(url.LocalPath);
                    }
                    catch (OutOfMemoryException oom)
                    {
                        Xaml2Emf.Logger?.LogWarning("Unsupported image format: {0}", oom.Message);
                    }
                    catch (FileNotFoundException fnf)
                    {
                        Xaml2Emf.Logger?.LogWarning("Image file not found: {0}", fnf.Message);
                    }
                else
                    Xaml2Emf.Logger?.LogWarning("Unable to access image: {0}", url);
            else
                Xaml2Emf.Logger?.LogWarning("Unable to resolve image: {0}", me);
            return null;
        }

        public static d.Brush ToGdiPlus(this LinearGradientBrush brush, Rect bounds)
        {
            d.Brush? db = CheckDegenerate(brush);
            if (db is not null) return db;
            var bt = new BrushTransform(brush, bounds);
            if (bt.DegenerateBrush is not null) return bt.DegenerateBrush;

            Point start = brush.StartPoint;
            Point end = brush.EndPoint;
            if (brush.MappingMode == BrushMappingMode.RelativeToBoundingBox)
            {
                start = bt.ToAbsolute.Transform(start);
                end = bt.ToAbsolute.Transform(end);
            }

            d2.WrapMode wm = brush.SpreadMethod.ToGdiPlus();
            if (wm == d2.WrapMode.Clamp)
            {
                wm = d2.WrapMode.TileFlipX;
                double delta = (bounds.BottomRight - bounds.TopLeft).Length
                               /(bt.ToBrush.Transform(end) - bt.ToBrush.Transform(start)).Length;
                Vector diff = delta*(end - start);
                start -= diff;
                end += diff;
                brush = brush.Clone();
                GradientStopCollection g = brush.GradientStops;
                g.Insert(0, new GradientStop(g[0].Color, -delta));
                g.Add(new GradientStop(g[g.Count - 1].Color, delta + 1));
            }

            var b = new d2.LinearGradientBrush(start.ToGdiPlus(), end.ToGdiPlus(), d.Color.Black, d.Color.White);
            b.InterpolationColors = ConvertGradient(brush);
            b.WrapMode = wm;
            b.MultiplyTransform(bt.ToBrush.ToGdiPlus(), d2.MatrixOrder.Append);
            return b;
        }

        public static d.Brush ToGdiPlus(this RadialGradientBrush brush, Rect bounds)
        {
            d.Brush? db = CheckDegenerate(brush);
            if (db is not null) return db;
            var bt = new BrushTransform(brush, bounds);
            if (bt.DegenerateBrush is not null) return bt.DegenerateBrush;

            Point center = brush.Center;
            Point focus = brush.GradientOrigin;
            var size = new Vector(brush.RadiusX, brush.RadiusY);
            if (brush.MappingMode == BrushMappingMode.RelativeToBoundingBox)
            {
                center = bt.ToAbsolute.Transform(center);
                focus = bt.ToAbsolute.Transform(focus);
                size = bt.ToAbsolute.Transform(size);
            }

            Vector ts = bt.ToBrush.Transform(size);
            var delta = (int) Math.Ceiling(4*(bounds.BottomRight - bounds.TopLeft).Length
                                           /Math.Min(Math.Abs(ts.X), Math.Abs(ts.Y)));
            size *= delta;
            center += (delta - 1)*(center - focus);
            brush = brush.Clone();
            GradientStopCollection g = brush.GradientStops;
            int last = g.Count - 1;
            double offset = 1.00000001;
            switch (brush.SpreadMethod)
            {
                case GradientSpreadMethod.Pad:
                    g.Add(new GradientStop(g[last].Color, delta));
                    break;
                case GradientSpreadMethod.Repeat:
                    for (int i = 0; i < delta; ++i)
                        for (int j = 0; j <= last; ++j)
                            g.Add(new GradientStop(g[j].Color, i + g[j].Offset + (j == last ? 1 : offset)));
                    break;
                case GradientSpreadMethod.Reflect:
                    for (int i = 0; i < delta; ++i)
                        if (i%2 == 0)
                            for (int j = 0; j <= last; ++j)
                                g.Add(new GradientStop(g[j].Color, i + (1 - g[j].Offset) + (j == 0 ? 1 : offset)));
                        else
                            for (int j = 0; j <= last; ++j)
                                g.Add(new GradientStop(g[j].Color, i + g[j].Offset + (j == last ? 1 : offset)));
                    break;
            }

            var b = new d2.PathGradientBrush(new EllipseGeometry(center, size.X, size.Y).ToGdiPlus());
            b.CenterPoint = focus.ToGdiPlus();
            b.InterpolationColors = ConvertGradient(brush);
            b.WrapMode = brush.SpreadMethod.ToGdiPlus();
            b.MultiplyTransform(bt.ToBrush.ToGdiPlus(), d2.MatrixOrder.Append);
            return b;
        }

        public static d.Brush ToGdiPlus(this ImageBrush brush, Rect bounds)
        {
            ImageSource img = brush.ImageSource;
            var bt = new BrushTransform(brush, bounds);
            if (bt.DegenerateBrush is not null) return bt.DegenerateBrush;

            Rect viewbox = brush.Viewbox;
            if (brush.ViewboxUnits == BrushMappingMode.RelativeToBoundingBox)
                viewbox.Scale(img.Width, img.Height);
            Rect viewport = brush.Viewport;
            if (brush.ViewportUnits == BrushMappingMode.RelativeToBoundingBox)
                viewport.Transform(bt.ToAbsolute);

            var ia = new di.ImageAttributes();
            ia.SetColorMatrix(new di.ColorMatrix {Matrix33 = (float) brush.Opacity});
            var i = img.ToGdiPlus();
            if (i is null) throw new InvalidOperationException();
            var b = new d.TextureBrush(i, viewbox.ToGdiPlus(), ia);
            b.WrapMode = brush.TileMode.ToGdiPlus();

            b.TranslateTransform((float) viewport.X, (float) viewport.Y);
            b.ScaleTransform((float) (viewport.Width/viewbox.Width), (float) (viewport.Height/viewbox.Height));
            b.MultiplyTransform(bt.ToBrush.ToGdiPlus(), d2.MatrixOrder.Append);

            return b;
        }

        #endregion

        #region private functions

        private static d.Brush? CheckDegenerate(GradientBrush brush)
        {
            switch (brush.GradientStops.Count)
            {
                case 0:
                    return new d.SolidBrush(d.Color.FromArgb(0, 255, 255, 255));
                case 1:
                    return new d.SolidBrush(brush.GradientStops[0].Color.ToGdiPlus(brush.Opacity));
            }
            return null;
        }

        private static d2.ColorBlend ConvertGradient(GradientBrush brush)
        {
            var g = new List<GradientStop>(brush.GradientStops);
            g.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            if (g[0].Offset > 0) g.Insert(0, new GradientStop(g[0].Color, 0));
            if (g[g.Count - 1].Offset < 1) g.Add(new GradientStop(g[g.Count - 1].Color, 1));

            double offset = g[0].Offset;
            if (offset < 0) foreach (GradientStop s in g) s.Offset -= offset;
            double scale = g[g.Count - 1].Offset;
            if (scale > 1) foreach (GradientStop s in g) s.Offset /= scale;

            var cb = new d2.ColorBlend(g.Count);
            bool invert = brush is RadialGradientBrush;
            for (int i = 0; i < g.Count; ++i)
            {
                cb.Positions[i] = (float) (invert ? (1 - g[i].Offset) : g[i].Offset);
                cb.Colors[i] = g[i].Color.ToGdiPlus(brush.Opacity);
            }
            if (invert)
            {
                Array.Reverse(cb.Positions);
                Array.Reverse(cb.Colors);
            }
            return cb;
        }

        #endregion

        [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
        private class BrushTransform
        {
            #region construction and destruction

            internal BrushTransform(Brush brush, Rect bounds)
            {
                ToAbsolute = Matrix.Identity;
                ToAbsolute.Scale(bounds.Width, bounds.Height);
                ToAbsolute.Translate(bounds.X, bounds.Y);
                Matrix fromAbsolute = ToAbsolute;
                fromAbsolute.Invert();
                ToBrush = fromAbsolute*brush.RelativeTransform.Value*ToAbsolute*brush.Transform.Value;
                if (!ToBrush.HasInverse)
                {
                    var dv = new DrawingVisual();
                    using (DrawingContext dc = dv.RenderOpen()) dc.DrawRectangle(brush, null, new Rect(0, 0, 1, 1));
                    var rtb = new RenderTargetBitmap(1, 1, 0, 0, PixelFormats.Pbgra32);
                    rtb.Render(dv);
                    var c = new byte[4];
                    rtb.CopyPixels(c, 4, 0);
                    DegenerateBrush = new d.SolidBrush(d.Color.FromArgb(c[3], c[2], c[1], c[0]));
                }
            }

            #endregion

            #region internal functions

            internal readonly Matrix ToAbsolute, ToBrush;
            internal readonly d.Brush? DegenerateBrush;

            #endregion
        }
    }

    internal static class GeometryExtensions
    {
        #region public functions

        public static d2.GraphicsPath ToGdiPlus(this Geometry geo)
        {
            PathGeometry pg = PathGeometry.CreateFromGeometry(geo);
            var path = new d2.GraphicsPath();
            path.FillMode = pg.FillRule.ToGdiPlus();
            foreach (PathFigure pf in pg.Figures)
            {
                if (!pf.IsFilled)
                    Xaml2Emf.Logger?.LogWarning("Unfilled path figures not supported, use null brush instead.");
                path.StartFigure();
                d.PointF lastPoint = pf.StartPoint.ToGdiPlus();
                foreach (PathSegment ps in pf.Segments)
                    lastPoint = ps.AddToPath(lastPoint, path);
                if (pf.IsClosed) path.CloseFigure();
            }
            if (pg.Transform is not null && !pg.Transform.Value.IsIdentity)
                path.Transform(pg.Transform.Value.ToGdiPlus());
            return path;
        }

        public static d2.FillMode ToGdiPlus(this FillRule me)
        {
            if (me == FillRule.EvenOdd)
                return d2.FillMode.Alternate;
            return d2.FillMode.Winding;
        }

        #endregion
    }

    internal static class SegmentExtensions
    {
        #region public functions

        public static d.PointF AddToPath(this PathSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {            
            if (!segment.IsStroked)
                Xaml2Emf.Logger?.LogWarning("Unstroked path segments not supported, use null pen instead.");
            // Except that they are used unecessarily on beziers auto-generated from arcs.
            //if (ps.IsSmoothJoin)
            //	Warning("Smooth join path segments not supported, use Pen.LineJoin=Round instead.");

            if (segment is ArcSegment a)
                startPoint = a.AddToPath(startPoint, path);
            else if (segment is BezierSegment b)
                startPoint = b.AddToPath(startPoint, path);
            else if (segment is LineSegment l)
                startPoint = l.AddToPath(startPoint, path);
            else if (segment is PolyBezierSegment pb)
                startPoint = pb.AddToPath(startPoint, path);
            else if (segment is PolyLineSegment pl)
                startPoint = pl.AddToPath(startPoint, path);
            else if (segment is PolyQuadraticBezierSegment pqb)
                startPoint = pqb.AddToPath(startPoint, path);
            else if (segment is QuadraticBezierSegment qb)
                startPoint = qb.AddToPath(startPoint, path);
            else
                throw new ArgumentOutOfRangeException("segment", segment.GetType().ToString());
            return startPoint;
        }

        public static d.PointF ToGdiPlus(this Point point)
        {
            return new d.PointF((float) point.X, (float) point.Y);
        }

        public static d.RectangleF ToGdiPlus(this Rect rect)
        {
            return new d.RectangleF((float) rect.X, (float) rect.Y, (float) rect.Width, (float) rect.Height);
        }

        public static d2.Matrix ToGdiPlus(this Matrix matrix)
        {
            return new d2.Matrix((float) matrix.M11, (float) matrix.M12,
                (float) matrix.M21, (float) matrix.M22,
                (float) matrix.OffsetX, (float) matrix.OffsetY);
        }

        #endregion

        #region private functions

        private static d.PointF AddToPath(this LineSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {
            d.PointF lastPoint = segment.Point.ToGdiPlus();
            path.AddLine(startPoint, lastPoint);
            return lastPoint;
        }

        private static d.PointF AddToPath(this PolyLineSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {
            var points = new d.PointF[segment.Points.Count + 1];
            int i = 0;
            points[i++] = startPoint;
            foreach (Point p in segment.Points) points[i++] = p.ToGdiPlus();
            path.AddLines(points);
            return points[i - 1];
        }

        private static d.PointF AddToPath(this BezierSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {
            d.PointF lastPoint = segment.Point3.ToGdiPlus();
            path.AddBezier(startPoint, segment.Point1.ToGdiPlus(), segment.Point2.ToGdiPlus(), lastPoint);
            return lastPoint;
        }

        private static d.PointF AddToPath(this PolyBezierSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {
            var points = new d.PointF[segment.Points.Count + 1];
            int i = 0;
            points[i++] = startPoint;
            foreach (Point p in segment.Points) points[i++] = p.ToGdiPlus();
            path.AddBeziers(points);
            return points[points.Length - 1];
        }

        private static d.PointF AddToPath(this QuadraticBezierSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {
            var c = new d.PointF[3];
            QuadraticToCubic(startPoint, segment.Point1.ToGdiPlus(), segment.Point2.ToGdiPlus(), c, 0);
            path.AddBezier(startPoint, c[0], c[1], c[2]);
            return c[2];
        }

        private static d.PointF AddToPath(this PolyQuadraticBezierSegment segment, d.PointF startPoint,
            d2.GraphicsPath path)
        {
            var points = new d.PointF[3*segment.Points.Count/2 + 1];
            int j = 0;
            points[j++] = startPoint;
            for (int i = 0; i < segment.Points.Count; i += 2)
            {
                QuadraticToCubic(points[j - 1], segment.Points[i].ToGdiPlus(), segment.Points[i + 1].ToGdiPlus(), points,
                    j);
                j += 3;
            }
            path.AddBeziers(points);
            return points[points.Length - 1];
        }

        private static void QuadraticToCubic(d.PointF q0, d.PointF q1, d.PointF q2, d.PointF[] c, int index)
        {
            c[index + 0].X = q0.X + 2*(q1.X - q0.X)/3;
            c[index + 0].Y = q0.Y + 2*(q1.Y - q0.Y)/3;
            c[index + 1].X = q1.X + (q2.X - q1.X)/3;
            c[index + 1].Y = q1.Y + (q2.Y - q1.Y)/3;
            c[index + 2].X = q2.X;
            c[index + 2].Y = q2.Y;
        }

        private static d.PointF AddToPath(this ArcSegment segment, d.PointF startPoint, d2.GraphicsPath path)
        {
            var pg = new PathGeometry
            {
                Figures = new PathFigureCollection
                {
                    new PathFigure
                    {
                        IsFilled = true,
                        IsClosed = true,
                        StartPoint = new Point(startPoint.X, startPoint.Y),
                        Segments = new PathSegmentCollection {segment}
                    }
                }
            };
            Rect r = pg.Bounds;
            r.Inflate(1, 1);
            PathGeometry g = Geometry.Combine(new RectangleGeometry(r), pg, GeometryCombineMode.Intersect,
                Transform.Identity);
            if (g.Figures.Count != 1)
                throw new InvalidOperationException("Geometry.Combine produced too many figures.");
            PathFigure pf = g.Figures[0];
            if (!(pf.Segments[0] is LineSegment))
                throw new InvalidOperationException("Geometry.Combine didn't start with a line");
            d.PointF lastPoint = startPoint;
            for (int i = 1; i < pf.Segments.Count; ++i)
            {
                if (pf.Segments[i] is ArcSegment)
                    throw new InvalidOperationException("Geometry.Combine produced an ArcSegment - oops, bad hack");
                lastPoint = pf.Segments[i].AddToPath(lastPoint, path);
            }
            return lastPoint;
        }

        #endregion
    }
}