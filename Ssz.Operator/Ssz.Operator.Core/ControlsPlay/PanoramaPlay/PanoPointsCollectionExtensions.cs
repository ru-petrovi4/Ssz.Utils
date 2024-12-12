using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using QuickGraph;
using QuickGraph.Algorithms;

using Ssz.Operator.Core.Panorama;
using Ssz.Operator.Core.Properties;
using Ssz.Utils;

namespace Ssz.Operator.Core.ControlsPlay.PanoramaPlay
{
    public static class PanoPointsCollectionExtensions
    {
        #region public functions

        public static Model3DGroup ImportFullMap(this PanoPointsCollection panoPointsCollection)
        {
            var result = new Model3DGroup();
            foreach (PanoPoint p in panoPointsCollection.PanoPoints)
            {
                if (!p.HasValidCoordinate()) continue;

                if (p.DsPageName == panoPointsCollection.StartDsPageName)
                    result.Children.Add(CreatePoint(p.X, p.Y, p.Z, 1.5, p.Material, p.DsPageName, 5.0));
                else
                    result.Children.Add(CreatePoint(p.X, p.Y, p.Z, 1.5, p.Material, p.DsPageName, 5.0));

                foreach (PanoPointRef r in p.PanoPointRefs)
                {
                    if (!r.HasValidVector()) continue;

                    if (r.MutualPanoPointRef is not null && r.HorizontalAngle > Math.PI) continue;

                    string linkName = r.ParentPanoPoint.DsPageName + " <--> " + r.ToPanoPoint.DsPageName;
                    result.Children.Add(CreateLink(r, 0.1,
                        r.Material, "", 5.0));
                }
            }

            return result;
        }


        public static Model3DGroup ImportNearPoint(this PanoPointsCollection panoPointsCollection,
            PanoPoint userAtPanoPoint)
        {
            var result = new Model3DGroup();

            var points = new CaseInsensitiveDictionary<PanoPoint>();

            GetNearestPoints(userAtPanoPoint, 2, points);

            foreach (PanoPoint p in points.Values)
                //result.Children.Add(CreatePoint(p.X, p.Y, p.Z, 0.1, p.Material, p.DsPageName));

            foreach (PanoPointRef r in p.PanoPointRefs)
            {
                if (!r.HasValidVector()) continue;
                //if (r.MutualPanoPointRef is not null && r.HorizontalAngle > Math.PI) continue;

                result.Children.Add(CreateLink(r, 0.1,
                    r.Material, ""));
            }

            return result;
        }

        public static Rect GetBoundingRect(this PanoPointsCollection panoPointsCollection)
        {
            if (panoPointsCollection.PanoPoints.Count == 0) return new Rect();

            var minX = double.MaxValue;
            var minY = double.MaxValue;
            var maxX = double.MinValue;
            var maxY = double.MinValue;
            foreach (var p in panoPointsCollection.PanoPoints)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new Rect(new Point(minX, minY), new Point(maxX, maxY));
        }


        public static bool ShowPath(this PanoPointsCollection panoPointsCollection, string destinationDsPageName)
        {
            PanoPoint? toPanoPoint = null;
            if (!string.IsNullOrWhiteSpace(destinationDsPageName))
                toPanoPoint = panoPointsCollection.PanoPointsDictionary.TryGetValue(destinationDsPageName);
            PanoPoint? fromPanoPoint = null;
            if (panoPointsCollection.CurrentPoint is not null)
            {
                fromPanoPoint = panoPointsCollection.CurrentPoint;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(panoPointsCollection.StartDsPageName))
                    fromPanoPoint =
                        panoPointsCollection.PanoPointsDictionary.TryGetValue(panoPointsCollection.StartDsPageName);
            }

            if (fromPanoPoint == toPanoPoint)
            {
                MessageBoxHelper.ShowInfo(OperatorUIResources.FindPathSuccededYouInDestinationPoint);
                return true;
            }

            foreach (PanoPoint p in panoPointsCollection.PanoPoints)
            foreach (PanoPointRef r in p.PanoPointRefs)
                r.Material.Brush = new SolidColorBrush(Colors.Cyan);

            var currentPath = new List<PanoPointRef>();

            if (fromPanoPoint is not null && toPanoPoint is not null)
            {
                var graph = new UndirectedGraph<int, Edge<int>>();
                for (var i = 0; i < panoPointsCollection.PanoPoints.Count; i += 1) graph.AddVertex(i);
                for (var i = 0; i < panoPointsCollection.PanoPoints.Count; i += 1)
                {
                    PanoPoint p = panoPointsCollection.PanoPoints[i];
                    foreach (PanoPointRef r in p.PanoPointRefs) graph.AddEdge(new Edge<int>(i, r.ToPanoPoint.Index));
                }

                Func<Edge<int>, double> pointsDistances = edge =>
                    panoPointsCollection.PanoPoints[edge.Source].PanoPointRefs.Find(
                        r => r.ToPanoPoint.Index == edge.Target)!.HorizontalLength;

                TryFunc<int, IEnumerable<Edge<int>>> tryGetPath = graph.ShortestPathsDijkstra(pointsDistances,
                    fromPanoPoint.Index);

                IEnumerable<Edge<int>> path;
                if (tryGetPath(toPanoPoint.Index, out path))
                {
                    var list = new List<Tuple<int, int>>();
                    var p = path.ToArray();
                    if (p.Length > 1)
                    {
                        var p0 = p[0];
                        var p1 = p[1];
                        if (p0.Target == p1.Source || p0.Target == p1.Target)
                            list.Add(Tuple.Create(p0.Source, p0.Target));
                        else if (p0.Source == p1.Source || p0.Source == p1.Target)
                            list.Add(Tuple.Create(p0.Target, p0.Source));
                        for (var i = 0; i < p.Length - 1; i += 1)
                        {
                            p0 = p[i];
                            p1 = p[i + 1];
                            if (p0.Source == p1.Source || p0.Target == p1.Source)
                                list.Add(Tuple.Create(p1.Source, p1.Target));
                            else if (p0.Source == p1.Target || p0.Target == p1.Target)
                                list.Add(Tuple.Create(p1.Target, p1.Source));
                        }
                    }
                    else if (p.Length == 1)
                    {
                        list.Add(Tuple.Create(fromPanoPoint.Index, toPanoPoint.Index));
                    }

                    foreach (var t in list)
                    {
                        var panoPointRef = panoPointsCollection.PanoPoints[t.Item1].PanoPointRefs.Find(
                            r => r.ToPanoPoint.Index == t.Item2);
                        if (panoPointRef is null) throw new InvalidOperationException();
                        panoPointRef.Material.Brush = BlinkingDsBrush.GetBrush(Colors.Cyan, Colors.Yellow);
                        if (panoPointRef.MutualPanoPointRef is not null)
                            panoPointRef.MutualPanoPointRef.Material.Brush =
                                BlinkingDsBrush.GetBrush(Colors.Cyan, Colors.Yellow);

                        panoPointRef.IndexInPath = currentPath.Count;
                        currentPath.Add(panoPointRef);
                    }

                    panoPointsCollection.CurrentPath = currentPath.ToArray();

                    MessageBoxHelper.ShowInfo(OperatorUIResources.FindPathSucceded);

                    return true;
                }
            }

            panoPointsCollection.CurrentPath = new PanoPointRef[0];

            MessageBoxHelper.ShowError(OperatorUIResources.FindPathFailed);

            return false;
        }

        public static void SetCurrentPoint(this PanoPointsCollection panoPointsCollection, PanoPoint panoPoint)
        {
            if (panoPointsCollection.CurrentPoint == panoPoint) return;
            panoPointsCollection.CurrentPoint = panoPoint;

            var currentPointDsPageName = panoPoint is not null ? panoPoint.DsPageName : null;
            foreach (PanoPoint p in panoPointsCollection.PanoPoints)
                if (p.DsPageName == currentPointDsPageName)
                    p.Material.Brush = new SolidColorBrush(Colors.Chartreuse);
                else
                    p.Material.Brush = null;
            /*
                 * if (p.DsPageName == panoPointsCollection.StartDsPageName)
                {
                    p.Material.Brush = new SolidColorBrush(Colors.Chartreuse);
                }                
                else */
        }


        public static int CurrentPathIndex(this PanoPointsCollection panoPointsCollection, string dsPageName)
        {
            if (panoPointsCollection.CurrentPath.Length == 0) return -1;
            if (StringHelper.CompareIgnoreCase(panoPointsCollection.CurrentPath[0].ParentPanoPoint.DsPageName,
                dsPageName)) return 0;
            var panoPointRef =
                panoPointsCollection.CurrentPath.LastOrDefault(r =>
                    StringHelper.CompareIgnoreCase(r.ToDsPageName, dsPageName));
            if (panoPointRef is null) return -1;
            return panoPointRef.IndexInPath + 1;
        }

        #endregion

        #region private functions

        private static void GetNearestPoints(PanoPoint point, int maxLinkCount,
            CaseInsensitiveDictionary<PanoPoint> points)
        {
            if (point is null) return;

            points[point.DsPageName] = point;

            maxLinkCount--;
            if (maxLinkCount < 1) return;

            foreach (PanoPointRef r in point.PanoPointRefs) GetNearestPoints(r.ToPanoPoint, maxLinkCount, points);
        }


        private static Model3DGroup CreatePoint(double x, double y, double z, double a, Material material, string name,
            double hMultiplier = 1.0)
        {
            z = hMultiplier * z;

            var group = new Model3DGroup();

            if (!string.IsNullOrWhiteSpace(name)) group.SetName(name);

            var p1 = new Point3D(x - a / 2, y + a / 2, z - a / 2);
            var p2 = new Point3D(x + a / 2, y, z - a / 2);
            var p3 = new Point3D(x - a / 2, y - a / 2, z - a / 2);
            var p4 = new Point3D(x, y, z + a / 2);

            group.Children.Add(CreateTriangle(p1, p2, p3, material));
            group.Children.Add(CreateTriangle(p1, p3, p4, material));
            group.Children.Add(CreateTriangle(p2, p4, p3, material));
            group.Children.Add(CreateTriangle(p1, p4, p2, material));

            return group;
        }


        private static Model3DGroup CreateLink(PanoPointRef panoPointRef, double a,
            Material material, string name, double hMultiplier = 1.0)
        {
            var angle = panoPointRef.HorizontalAngle;

            var x1 = panoPointRef.ParentPanoPoint.X; // + a*1.5*Math.Cos(angle);
            var y1 = panoPointRef.ParentPanoPoint.Y; // + a*1.5*Math.Sin(angle);
            var z1 = panoPointRef.ParentPanoPoint.Z * hMultiplier;

            var horizontalLength = panoPointRef.HorizontalLength; // - 3*a;

            var x2 = x1 + horizontalLength * Math.Cos(angle);
            var y2 = y1 + horizontalLength * Math.Sin(angle);
            var z2 = z1 + panoPointRef.VerticalDelta * hMultiplier;

            var group = new Model3DGroup();

            if (!string.IsNullOrWhiteSpace(name)) group.SetName(name);

            var p1 = new Point3D(x1 + a / 2 * Math.Cos(angle + Math.PI / 2), y1 + a / 2 * Math.Sin(angle + Math.PI / 2),
                z1 - a / 2);
            var p2 = new Point3D(x1, y1, z1 + a / 2);
            var p3 = new Point3D(x1 + a / 2 * Math.Cos(angle - Math.PI / 2), y1 + a / 2 * Math.Sin(angle - Math.PI / 2),
                z1 - a / 2);
            var p4 = new Point3D(x2 + a / 2 * Math.Cos(angle + Math.PI / 2), y2 + a / 2 * Math.Sin(angle + Math.PI / 2),
                z2 - a / 2);
            var p5 = new Point3D(x2, y2, z2 + a / 2);
            var p6 = new Point3D(x2 + a / 2 * Math.Cos(angle - Math.PI / 2), y2 + a / 2 * Math.Sin(angle - Math.PI / 2),
                z2 - a / 2);

            //1
            group.Children.Add(CreateTriangle(p1, p5, p4, material));
            group.Children.Add(CreateTriangle(p2, p5, p1, material));

            //2
            group.Children.Add(CreateTriangle(p2, p6, p5, material));
            group.Children.Add(CreateTriangle(p3, p6, p2, material));

            //3
            group.Children.Add(CreateTriangle(p1, p4, p3, material));
            group.Children.Add(CreateTriangle(p3, p4, p6, material));

            return group;
        }

        private static Model3DGroup CreateTriangle(Point3D p0, Point3D p1, Point3D p2, Material material)
        {
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            var normal = CalcNormal(p0, p1, p2);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            var model = new GeometryModel3D(mesh, material);
            var group = new Model3DGroup();
            group.Children.Add(model);
            return group;
        }

        private static Vector3D CalcNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            var v0 = new Vector3D(p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            var v1 = new Vector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Vector3D.CrossProduct(v0, v1);
        }

        #endregion
    }
}

/*
        private static Model3DGroup CreateCube(double x, double y, double z, double a, double b, double c)
        {
            var group = new Model3DGroup();

            var p0 = new Point3D(0 + x, 0 + y, 0 + z);
            var p1 = new Point3D(a + x, 0 + y, 0 + z);
            var p2 = new Point3D(a + x, 0 + y, c + z);
            var p3 = new Point3D(0 + x, 0 + y, c + z);
            var p4 = new Point3D(0 + x, b + y, 0 + z);
            var p5 = new Point3D(a + x, b + y, 0 + z);
            var p6 = new Point3D(a + x, b + y, c + z);
            var p7 = new Point3D(0 + x, b + y, c + z);

            //front
            group.Children.Add(CreateTriangle(p3, p2, p6));
            group.Children.Add(CreateTriangle(p3, p6, p7));

            //right
            group.Children.Add(CreateTriangle(p2, p1, p5));
            group.Children.Add(CreateTriangle(p2, p5, p6));

            //back
            group.Children.Add(CreateTriangle(p1, p0, p4));
            group.Children.Add(CreateTriangle(p1, p4, p5));

            //left
            group.Children.Add(CreateTriangle(p0, p3, p7));
            group.Children.Add(CreateTriangle(p0, p7, p4));

            //top
            group.Children.Add(CreateTriangle(p7, p6, p5));
            group.Children.Add(CreateTriangle(p7, p5, p4));

            //bottom
            group.Children.Add(CreateTriangle(p2, p3, p0));
            group.Children.Add(CreateTriangle(p2, p0, p1));

            return group;
        }*/