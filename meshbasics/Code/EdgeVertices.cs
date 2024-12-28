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

    public static EdgeVertices TerraceLerp(
        EdgeVertices a, EdgeVertices b, int step) {
        EdgeVertices result;
        result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
        result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
        result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
        result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
        return result;
    }
}
