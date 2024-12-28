using Godot;

namespace JHM.MeshBasics;

public struct EdgeVertices {
    public Vector3 v1, v2, v3, v4;

    public EdgeVertices(Vector3 corner1, Vector3 corner2) {
        v1 = corner1;
        v2 = corner1.Lerp(corner2, 1.0f / 3.0f);
        v3 = corner1.Lerp(corner2, 2.0f / 3.0f);
        v4 = corner2;
    }
}
