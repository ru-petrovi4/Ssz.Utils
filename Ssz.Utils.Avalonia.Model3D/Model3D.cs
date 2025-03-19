using System.Numerics;

namespace Ssz.Utils.Avalonia.Model3D;

public class Model3D
{
    public Model3DScene? Model3DScene;
    public float RotationX;
    public float RotationY;
    public float Zoom;
}

public class Model3DScene
{
    public Point3DWithColor[]? Point3DWithColorArray;
}

public class Point3DWithColor
{
    public Vector3 Position { get; set; }
    public Vector4 Color { get; set; }
}
