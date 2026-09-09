using System;
using System.Numerics;
using Ssz.Operator.Core.DsPageTypes;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     The sphere or cylinder the panorama page is drawn on, as a triangle list with texture
    ///     coordinates. Port of PanoramaViewport3D.NewPanoramaGeometry: same segment counts and the same
    ///     position / texture coordinate formulas, so a page lands on the mesh exactly as it did in WPF.
    ///     Avalonia has no 3D, so this is plain data that PanoramaViewport projects and hands to Skia.
    /// </summary>
    public sealed class PanoramaMesh
    {
        #region construction and destruction

        private PanoramaMesh(Vector3[] positions, Vector2[] textureCoordinates, int[] triangleIndices)
        {
            Positions = positions;
            TextureCoordinates = textureCoordinates;
            TriangleIndices = triangleIndices;
        }

        #endregion

        #region public functions

        public const double CylinderUpDownAngle = 60;

        public Vector3[] Positions { get; }

        /// <summary>
        ///     Normalized, 0..1 over the whole panorama page.
        /// </summary>
        public Vector2[] TextureCoordinates { get; }

        public int[] TriangleIndices { get; }

        public static PanoramaMesh Get(PanoramaType panoramaType)
        {
            switch (panoramaType)
            {
                case PanoramaType.Cylindrical:
                    return _cylindricalMesh ??= New(PanoramaType.Cylindrical);
                default:
                    return _sphericalMesh ??= New(PanoramaType.Spherical);
            }
        }

        /// <summary>
        ///     The direction the given point of the page is seen in, before the camera rotation.
        ///     u and v are normalized page coordinates, the same ones the mesh carries as texture
        ///     coordinates, so a shape of the page can be projected exactly where its pixels land.
        /// </summary>
        public static Vector3 GetDirection(PanoramaType panoramaType, double u, double v)
        {
            // Both texture coordinate formulas of the WPF mesh are inverted here.
            var t = 2.0 * Math.PI * (1 - u);
            switch (panoramaType)
            {
                case PanoramaType.Cylindrical:
                {
                    var zMax = Math.Tan(CylinderUpDownAngle * 2.0 * Math.PI / 360.0);
                    var zMin = -zMax;
                    var z = zMax - v * (zMax - zMin);
                    return new Vector3((float) Math.Cos(t), (float) Math.Sin(t), (float) z);
                }
                default:
                {
                    var z = Math.Sin(Math.PI / 2 - v * Math.PI);
                    var r = Math.Sqrt(Math.Max(0.0, 1 - z * z));
                    return new Vector3((float) (r * Math.Cos(t)), (float) (r * Math.Sin(t)), (float) z);
                }
            }
        }

        /// <summary>
        ///     The inverse of GetDirection: the point of the page a direction points at, normalized.
        ///     WPF needed nothing of the kind, because the hit test of the Viewport3D routed the mouse to
        ///     the live page visual on the 3D surface; here the page is only a texture, so a click has to
        ///     be traced back to it by hand.
        /// </summary>
        public static bool TryGetPagePoint(PanoramaType panoramaType, Vector3 direction,
            out double u, out double v)
        {
            u = 0;
            v = 0;

            double x = direction.X;
            double y = direction.Y;
            double z = direction.Z;

            var r = Math.Sqrt(x * x + y * y);
            if (r < 1e-9) return false;

            var t = Math.Atan2(y, x);
            if (t < 0) t += 2.0 * Math.PI;
            u = 1 - t / (2.0 * Math.PI);
            if (u >= 1.0) u -= 1.0;

            switch (panoramaType)
            {
                case PanoramaType.Cylindrical:
                {
                    var zMax = Math.Tan(CylinderUpDownAngle * 2.0 * Math.PI / 360.0);
                    var zMin = -zMax;
                    // Where the ray meets the cylinder of radius 1.
                    var zOnCylinder = z / r;
                    if (zOnCylinder > zMax || zOnCylinder < zMin) return false;
                    v = (zMax - zOnCylinder) / (zMax - zMin);
                    break;
                }
                default:
                {
                    var length = Math.Sqrt(x * x + y * y + z * z);
                    if (length < 1e-9) return false;
                    var sin = z / length;
                    if (sin > 1.0) sin = 1.0;
                    else if (sin < -1.0) sin = -1.0;
                    v = (Math.PI / 2 - Math.Asin(sin)) / Math.PI;
                    break;
                }
            }

            return true;
        }

        #endregion

        #region private functions

        private static PanoramaMesh New(PanoramaType panoramaType)
        {
            double zMin = 0;
            double zMax = 0;

            switch (panoramaType)
            {
                case PanoramaType.Spherical:
                    zMax = 1;
                    zMin = -1;
                    break;
                case PanoramaType.Cylindrical:
                    zMax = Math.Tan(CylinderUpDownAngle * 2.0 * Math.PI / 360.0);
                    zMin = -zMax;
                    break;
            }

            var dt = 2.0 * Math.PI / TiMax;
            var dz = (zMax - zMin) / ZiMax;

            var count = (ZiMax + 1) * (TiMax + 1);
            var positions = new Vector3[count];
            var textureCoordinates = new Vector2[count];

            var i = 0;
            for (var zi = 0; zi <= ZiMax; zi += 1)
            {
                var z = zMin + zi * dz;

                for (var ti = 0; ti <= TiMax; ti += 1, i += 1)
                {
                    var t = ti * dt; // radians

                    switch (panoramaType)
                    {
                        case PanoramaType.Spherical:
                        {
                            var r = Math.Sqrt(Math.Max(0.0, 1 - z * z));
                            positions[i] = new Vector3((float) (r * Math.Cos(t)), (float) (r * Math.Sin(t)),
                                (float) z);
                            textureCoordinates[i] = new Vector2((float) (1 - t / (2.0 * Math.PI)),
                                (float) ((Math.PI / 2 - Math.Asin(z)) / Math.PI));
                        }
                            break;
                        case PanoramaType.Cylindrical:
                        {
                            positions[i] = new Vector3((float) Math.Cos(t), (float) Math.Sin(t), (float) z);
                            textureCoordinates[i] = new Vector2((float) (1 - t / (2.0 * Math.PI)),
                                (float) ((zMax - z) / (zMax - zMin)));
                        }
                            break;
                    }
                }
            }

            var triangleIndices = new int[ZiMax * TiMax * 6];
            var k = 0;
            for (var zi = 0; zi < ZiMax; zi += 1)
            for (var ti = 0; ti < TiMax; ti += 1)
            {
                var beginIndexZ0 = zi * (TiMax + 1);
                var beginIndexZ1 = (zi + 1) * (TiMax + 1);

                triangleIndices[k++] = beginIndexZ0 + ti + 1;
                triangleIndices[k++] = beginIndexZ0 + ti;
                triangleIndices[k++] = beginIndexZ1 + ti;

                triangleIndices[k++] = beginIndexZ1 + ti;
                triangleIndices[k++] = beginIndexZ1 + ti + 1;
                triangleIndices[k++] = beginIndexZ0 + ti + 1;
            }

            return new PanoramaMesh(positions, textureCoordinates, triangleIndices);
        }

        #endregion

        #region private fields

        private const int TiMax = 64;
        private const int ZiMax = 64;

        private static PanoramaMesh? _sphericalMesh;
        private static PanoramaMesh? _cylindricalMesh;

        #endregion
    }
}
