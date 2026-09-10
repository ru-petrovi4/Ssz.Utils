using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using Avalonia.Media;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    /// <summary>
    ///     The compass model of the panorama: the Wavefront file Resources/compass.obj that HelixToolkit
    ///     loaded for the WPF control, read here into plain triangles, because Avalonia has neither 3D nor
    ///     a model importer. Only what the file actually uses is read: vertices, normals, faces and the
    ///     diffuse color of the material they are drawn with.
    /// </summary>
    public sealed class CompassModel
    {
        #region construction and destruction

        private CompassModel(Triangle[] triangles)
        {
            Triangles = triangles;
        }

        #endregion

        #region public functions

        public readonly struct Triangle
        {
            public Triangle(Vector3 a, Vector3 b, Vector3 c, Vector3 normalA, Vector3 normalB, Vector3 normalC,
                Vector3 normal, Color color)
            {
                A = a;
                B = b;
                C = c;
                NormalA = normalA;
                NormalB = normalB;
                NormalC = normalC;
                Normal = normal;
                Color = color;
            }

            public Vector3 A { get; }
            public Vector3 B { get; }
            public Vector3 C { get; }

            /// <summary>
            ///     The normals of the corners, which is what makes the shading of a round surface smooth,
            ///     the way the 3D pipeline of WPF shaded it.
            /// </summary>
            public Vector3 NormalA { get; }
            public Vector3 NormalB { get; }
            public Vector3 NormalC { get; }

            /// <summary>
            ///     The normal of the face itself, used to tell whether it faces away.
            /// </summary>
            public Vector3 Normal { get; }

            public Color Color { get; }
        }

        public Triangle[] Triangles { get; }

        /// <summary>
        ///     The model, read once. Null when the file is missing or unreadable, and then the compass
        ///     simply is not drawn - as it was in WPF, where the importer failed silently.
        /// </summary>
        public static CompassModel? Get()
        {
            if (_loaded) return _model;
            _loaded = true;

            try
            {
                _model = Load(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Resources", @"compass.obj"));
            }
            catch (Exception)
            {
                _model = null;
            }

            return _model;
        }

        #endregion

        #region private functions

        private static CompassModel? Load(string objFileFullName)
        {
            if (!File.Exists(objFileFullName)) return null;

            var materials = ReadMaterials(Path.ChangeExtension(objFileFullName, @".mtl"));

            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var triangles = new List<Triangle>();

            var color = Colors.Gray;

            foreach (string line in File.ReadLines(objFileFullName))
            {
                string[] parts = line.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case @"v":
                        if (parts.Length >= 4) positions.Add(ParseVector(parts, 1));
                        break;
                    case @"vn":
                        if (parts.Length >= 4) normals.Add(ParseVector(parts, 1));
                        break;
                    case @"usemtl":
                        if (parts.Length >= 2 && materials.TryGetValue(parts[1], out var materialColor))
                            color = materialColor;
                        break;
                    case @"f":
                    {
                        if (parts.Length < 4) break;

                        // The faces of the file are triangles, quads and the two polygons of the disc,
                        // all of them convex, so a fan around the first vertex triangulates them.
                        for (var i = 2; i + 1 < parts.Length; i += 1)
                        {
                            var a = ParseFaceVertex(parts[1], positions, normals);
                            var b = ParseFaceVertex(parts[i], positions, normals);
                            var c = ParseFaceVertex(parts[i + 1], positions, normals);
                            if (a is null || b is null || c is null) continue;

                            var normal = a.Value.Normal + b.Value.Normal + c.Value.Normal;
                            if (normal.LengthSquared() < 1e-12f)
                                normal = Vector3.Cross(b.Value.Position - a.Value.Position,
                                    c.Value.Position - a.Value.Position);
                            if (normal.LengthSquared() < 1e-12f) continue;
                            normal = Vector3.Normalize(normal);

                            triangles.Add(new Triangle(a.Value.Position, b.Value.Position, c.Value.Position,
                                Normalize(a.Value.Normal, normal), Normalize(b.Value.Normal, normal),
                                Normalize(c.Value.Normal, normal), normal, color));
                        }
                    }
                        break;
                }
            }

            return triangles.Count == 0 ? null : new CompassModel(triangles.ToArray());
        }

        private static Dictionary<string, Color> ReadMaterials(string mtlFileFullName)
        {
            var result = new Dictionary<string, Color>();
            if (!File.Exists(mtlFileFullName)) return result;

            string name = @"";
            foreach (string line in File.ReadLines(mtlFileFullName))
            {
                string[] parts = line.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                if (parts[0] == @"newmtl" && parts.Length >= 2)
                    name = parts[1];
                else if (parts[0] == @"Kd" && parts.Length >= 4 && name != @"")
                    result[name] = Color.FromRgb(ToByte(parts[1]), ToByte(parts[2]), ToByte(parts[3]));
            }

            return result;
        }

        private static (Vector3 Position, Vector3 Normal)? ParseFaceVertex(string token, List<Vector3> positions,
            List<Vector3> normals)
        {
            // v, v/vt, v//vn or v/vt/vn, one based and negative from the end.
            string[] indices = token.Split('/');

            var position = GetAt(positions, indices.Length > 0 ? indices[0] : @"");
            if (position is null) return null;

            var normal = indices.Length > 2 ? GetAt(normals, indices[2]) : null;
            return (position.Value, normal ?? Vector3.Zero);
        }

        private static Vector3? GetAt(List<Vector3> list, string indexString)
        {
            if (!int.TryParse(indexString, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                return null;

            if (index > 0) index -= 1;
            else if (index < 0) index += list.Count;
            else return null;

            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }

        private static Vector3 Normalize(Vector3 normal, Vector3 fallback)
        {
            return normal.LengthSquared() < 1e-12f ? fallback : Vector3.Normalize(normal);
        }

        private static Vector3 ParseVector(string[] parts, int index)
        {
            return new Vector3(ToSingle(parts[index]), ToSingle(parts[index + 1]), ToSingle(parts[index + 2]));
        }

        private static float ToSingle(string value)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result)
                ? result
                : 0.0f;
        }

        private static byte ToByte(string value)
        {
            var result = ToSingle(value) * 255.0f;
            if (result < 0.0f) result = 0.0f;
            else if (result > 255.0f) result = 255.0f;
            return (byte) result;
        }

        #endregion

        #region private fields

        private static bool _loaded;
        private static CompassModel? _model;

        #endregion
    }
}
