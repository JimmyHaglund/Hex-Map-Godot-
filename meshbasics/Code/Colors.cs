using Godot;

namespace JHM.MeshBasics;
public static class Colors {
    public static Color Blue => new(0.1f, 0.1f, 0.9f);
    public static Color Red => new(0.9f, 0.1f, 0.1f);
    public static Color White => new(1.0f, 1.0f, 1.0f);
    public static Color FromVector3(Vector3 v) => new(v.X, v.Y, v.Z);
    public static Color FromVector4(Vector4 v) => new(v.X, v.Y, v.Z, v.Y);

}
