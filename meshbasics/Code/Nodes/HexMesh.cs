using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexMesh : MeshInstance3D {
    private static List<Vector3> _vertices = new();
    private static List<Color> _colors = new();
    private static List<Vector3> _normals = new();
    private ArrayMesh _mesh;
    // private List<int> _triangles = new();
    private CollisionShape3D _activeShape;
    private CollisionShape3D _inactiveShape;

    [Export] public CollisionShape3D CollisionShape { get; set; }
    [Export] public CollisionShape3D AltShape { get; set; }

    public override void _Ready() {
        _mesh = Mesh as ArrayMesh;
        _activeShape = CollisionShape;
        _inactiveShape = AltShape;
        if (_mesh is null) {
            GD.PrintErr("HexMesh requires an ArrayMesh.");
        }
    }

    public void Clear() {
        _mesh.ClearSurfaces();
        _vertices.Clear();
        _colors.Clear();
        _normals.Clear();
    }

    public void Apply() {
        var surfaceTool = new SurfaceTool();

        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetMaterial(this.GetActiveMaterial(0));

        for (var n = _vertices.Count - 1; n >= 0; n--) {
            var vertex = _vertices[n];
            surfaceTool.SetNormal(_normals[n]);
            surfaceTool.SetColor(_colors[n]);
            surfaceTool.AddVertex(vertex);
        }
        surfaceTool.Commit(_mesh);

        var shape = _mesh.CreateTrimeshShape();
        _inactiveShape.Shape = shape;
        CallDeferred("SwapCollisionShape");
    }

    public void SetVertices(List<Vector3> vertices) => _vertices = vertices;
    public void SetColors(List<Color> colors) => _colors = colors;
    public void SetNormals(List<Vector3> normals) => _normals = normals;

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        //int vertexIndex = _vertices.Count;
        var normal = (v2 - v1).Cross(v3 - v2);
        _vertices.Add(HexMetrics.Perturb(v1));
        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v3));
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleUnPerturbed(Vector3 v1, Vector3 v2, Vector3 v3) {
        var normal = (v2 - v1).Cross(v3 - v2);
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        // int vertexIndex = _vertices.Count;
        // triangles.Add(vertexIndex);
        // triangles.Add(vertexIndex + 1);
        // triangles.Add(vertexIndex + 2);
    }

    public void AddTriangleColor(Color color) {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    public void AddTriangleColor(Color c1, Color c2, Color c3) {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);

    }

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
        //int vertexIndex = _vertices.Count;
        var normal = (v2 - v1).Cross(v2 - v3);
        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v1));
        _vertices.Add(HexMetrics.Perturb(v3));

        _vertices.Add(HexMetrics.Perturb(v2));
        _vertices.Add(HexMetrics.Perturb(v3));
        _vertices.Add(HexMetrics.Perturb(v4));

        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);
        // _vertices.Add(v4);

        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 2);

        // _triangles.Add(vertexIndex + 2);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 3);

    }

    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);

        _colors.Add(c2);
        _colors.Add(c3);
        _colors.Add(c4);
    }

    public void AddQuadColor(Color c1, Color c2) {
        _colors.Add(c1);
        _colors.Add(c1);
        _colors.Add(c2);

        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c2);
    }

    public void AddQuadColor(Color color) {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private void SwapCollisionShape() {
        var deactivated = _activeShape;
        var activated = _inactiveShape;
        _activeShape = activated;
        _inactiveShape = deactivated;
        activated.ProcessMode = ProcessModeEnum.Inherit;
        deactivated.ProcessMode = ProcessModeEnum.Disabled;
    }
}
