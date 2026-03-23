using System.Collections.Generic;
using System.Numerics;

namespace Ssz.Utils.Avalonia.Model3D;


public class Model3DScene
{
    public List<Point3DWithColor>? Points;

    public List<List<Point3DWithColor>>? Lines;
}

public class Point3DWithColor
{
    public Vector3 Position;
    public Vector4 Color;
}
