using System.Numerics;

namespace Ssz.Utils.Avalonia.Model3D;


public class Model3DScene
{
    public Point3DWithColor[]? Point3DWithColorArray;
}

public class Point3DWithColor
{
    public Vector3 Position;
    public Vector4 Color;
}
