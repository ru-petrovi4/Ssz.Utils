using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     The compass in the corner of a panorama, turning with the view.
    ///     WPF put the model on a Viewport3D with a perspective camera and one directional light; the same
    ///     camera and the same light are computed here and the shaded triangles are handed to Skia,
    ///     because Avalonia has no 3D.
    /// </summary>
    public class CompassControl : Control
    {
        #region public functions

        public void SetViewAzimuth(double viewAzimuth)
        {
            if (Math.Abs(_viewAzimuth - viewAzimuth) < 0.01) return;

            _viewAzimuth = viewAzimuth;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var model = CompassModel.Get();
            if (model is null) return;

            var size = Bounds.Size;
            if (size.Width <= 0 || size.Height <= 0) return;

            var rotation = Matrix4x4.CreateRotationY((float) (_viewAzimuth * Math.PI / 180.0));
            var scale = size.Width / 2 / Math.Tan(FieldOfView * Math.PI / 360.0);

            // Nothing depth sorts for us, so the faces are drawn from the back; with the ones that face
            // away left out, that is right for a body as simple as this one.
            var faces = new List<(double Depth, SKPoint[] Points, SKColor[] Colors)>(model.Triangles.Length);

            foreach (var triangle in model.Triangles)
            {
                var a = Vector3.Transform(triangle.A, rotation);
                var b = Vector3.Transform(triangle.B, rotation);
                var c = Vector3.Transform(triangle.C, rotation);

                var normal = Vector3.TransformNormal(triangle.Normal, rotation);
                if (Vector3.Dot(normal, (a + b + c) / 3 - CameraPosition) > 0) continue;

                var viewA = ToView(a);
                var viewB = ToView(b);
                var viewC = ToView(c);
                if (viewA.Z <= NearPlane || viewB.Z <= NearPlane || viewC.Z <= NearPlane) continue;

                faces.Add(((viewA.Z + viewB.Z + viewC.Z) / 3.0,
                    new[]
                    {
                        Project(viewA, size, scale), Project(viewB, size, scale), Project(viewC, size, scale)
                    },
                    new[]
                    {
                        Shade(triangle.Color, Vector3.TransformNormal(triangle.NormalA, rotation)),
                        Shade(triangle.Color, Vector3.TransformNormal(triangle.NormalB, rotation)),
                        Shade(triangle.Color, Vector3.TransformNormal(triangle.NormalC, rotation))
                    }));
            }

            faces.Sort((x, y) => y.Depth.CompareTo(x.Depth));

            var points = new SKPoint[faces.Count * 3];
            var colors = new SKColor[faces.Count * 3];
            for (var i = 0; i < faces.Count; i += 1)
            {
                Array.Copy(faces[i].Points, 0, points, i * 3, 3);
                Array.Copy(faces[i].Colors, 0, colors, i * 3, 3);
            }

            if (points.Length == 0) return;

            context.Custom(new CompassDrawOperation(new Rect(size), points, colors));
        }

        #endregion

        #region private functions

        /// <summary>
        ///     The camera of the WPF control: at (0, 15, 15), looking at the origin, with Y up.
        /// </summary>
        private static Vector3 ToView(Vector3 position)
        {
            var d = position - CameraPosition;
            return new Vector3(Vector3.Dot(d, CameraRight), Vector3.Dot(d, CameraUp), Vector3.Dot(d, CameraForward));
        }

        private static SKPoint Project(Vector3 viewPosition, Size size, double scale)
        {
            return new SKPoint((float) (size.Width / 2 + viewPosition.X / viewPosition.Z * scale),
                (float) (size.Height / 2 - viewPosition.Y / viewPosition.Z * scale));
        }

        /// <summary>
        ///     The one light of the WPF scene: white, shining straight down, and no ambient light at all.
        /// </summary>
        private static SKColor Shade(Color color, Vector3 normal)
        {
            var light = Math.Max(0.0f, normal.Y);
            return new SKColor((byte) (color.R * light), (byte) (color.G * light), (byte) (color.B * light),
                color.A);
        }

        #endregion

        #region private fields

        private const double FieldOfView = 45;
        private const float NearPlane = 0.01f;

        private static readonly Vector3 CameraPosition = new(0, 15, 15);
        private static readonly Vector3 CameraForward = Vector3.Normalize(new Vector3(0, -15, -15));
        private static readonly Vector3 CameraRight = new(1, 0, 0);
        private static readonly Vector3 CameraUp = Vector3.Normalize(new Vector3(0, 15, -15));

        private double _viewAzimuth;

        #endregion

        #region private classes

        private sealed class CompassDrawOperation : ICustomDrawOperation
        {
            public CompassDrawOperation(Rect bounds, SKPoint[] points, SKColor[] colors)
            {
                Bounds = bounds;
                _points = points;
                _colors = colors;
            }

            public Rect Bounds { get; }

            public void Dispose()
            {
            }

            public bool Equals(ICustomDrawOperation? other) => false;

            public bool HitTest(Point p) => false;

            public void Render(ImmediateDrawingContext context)
            {
                var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
                if (leaseFeature is null) return;

                using var lease = leaseFeature.Lease();
                var canvas = lease.SkCanvas;

                // Without a shader the colors of the vertices are blended with the color of the paint,
                // so the paint has to be white for them to come through unchanged.
                using var paint = new SKPaint { IsAntialias = true, Color = SKColors.White };
                canvas.DrawVertices(SKVertexMode.Triangles, _points, _colors, paint);
            }

            private readonly SKPoint[] _points;
            private readonly SKColor[] _colors;
        }

        #endregion
    }
}
