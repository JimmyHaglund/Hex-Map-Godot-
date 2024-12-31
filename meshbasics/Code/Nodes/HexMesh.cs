using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexMesh : MeshInstance3D {
    private List<Vector3> _vertices = new();
    private List<Color> _colors = new();
    private List<Vector3> _normals = new();
    private ArrayMesh _mesh;
    // private List<int> _triangles = new();
    private CollisionShape3D _activeShape;
    private CollisionShape3D _inactiveShape;
    private List<Vector2> _uvs;
    private List<Vector2> _uv2s;
    private List<Vector3> _terrainTypes = new();

    [Export] public CollisionShape3D CollisionShape { get; set; }
    [Export] public CollisionShape3D AltShape { get; set; }
    [Export] public bool UseCollider { get; set; } = true;
    [Export] public bool UseColors { get; set; } = true;
    [Export] public bool UseUVCoordinates { get; set; } = false;
    [Export] public bool UseUV2Coordinates { get; set; } = false;
    [Export] public bool UseTerrainTypes { get; set; } = false;

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
        _vertices = ListPool<Vector3>.Get();
        _normals = ListPool<Vector3>.Get();

        if (UseUVCoordinates) {
            _uvs = ListPool<Vector2>.Get();
        }
        if (UseUV2Coordinates) {
            _uv2s = ListPool<Vector2>.Get();
        }

        if (UseColors) {
            _colors = ListPool<Color>.Get();
        }
        if (UseTerrainTypes) {
            _terrainTypes = ListPool<Vector3>.Get();
        }
    }

    public void Apply() {
        var surfaceTool = new SurfaceTool();

        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetMaterial(this.GetActiveMaterial(0));
        surfaceTool.SetCustomFormat(0, SurfaceTool.CustomFormat.RgbFloat);
        for (var n = _vertices.Count - 1; n >= 0; n--) {
            var vertex = _vertices[n];
            surfaceTool.SetNormal(_normals[n]);

            if (UseTerrainTypes) {
                var terrain = _terrainTypes[n];
                surfaceTool.SetCustom(0, new(terrain.X, terrain.Y, terrain.Z));
            }
            if (UseUVCoordinates) {
                var uv = Vector2.Zero;
                if (_uvs != null && n < _uvs.Count) uv = _uvs[n];
                surfaceTool.SetUV(uv);
            }
            if (UseUV2Coordinates) {
                var uv = Vector2.Zero;
                if (_uv2s != null && n < _uv2s.Count) uv = _uv2s[n];
                surfaceTool.SetUV2(uv);
            }
            if (UseColors) {
                surfaceTool.SetColor(_colors[n]);
            }
            surfaceTool.AddVertex(vertex);
        }
        surfaceTool.Commit(_mesh);

        ListPool<Vector3>.Add(_vertices);
        ListPool<Vector3>.Add(_normals);
        
        if (UseColors) {
            ListPool<Color>.Add(_colors);
        }
        if (UseUVCoordinates) {
            ListPool<Vector2>.Add(_uvs);
        }
        if (UseTerrainTypes) {
            ListPool<Vector3>.Add(_terrainTypes);
        }

        if (!UseCollider) return;
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

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3) {
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

    public void AddQuadUnperturbed(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4
    ) {
        var normal = (v2 - v1).Cross(v2 - v3);
        _vertices.Add(v2);
        _vertices.Add(v1);
        _vertices.Add(v3);

        _vertices.Add(v2);
        _vertices.Add(v3);
        _vertices.Add(v4);

        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        // int vertexIndex = _vertices.Count;
        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 2);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 2);
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

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3) {
        _uvs.Add(uv1);
        _uvs.Add(uv2);
        _uvs.Add(uv3);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) {
        _uvs.Add(uv2);
        _uvs.Add(uv1);
        _uvs.Add(uv3);

        _uvs.Add(uv2);
        _uvs.Add(uv3);
        _uvs.Add(uv4);
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax) {
        _uvs.Add(new Vector2(uMax, vMin));
        _uvs.Add(new Vector2(uMin, vMin));
        _uvs.Add(new Vector2(uMin, vMax));

        _uvs.Add(new Vector2(uMax, vMin));
        _uvs.Add(new Vector2(uMin, vMax));
        _uvs.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3) {
        _uv2s.Add(uv1);
        _uv2s.Add(uv2);
        _uv2s.Add(uv3);
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) {
        _uv2s.Add(uv2);
        _uv2s.Add(uv1);
        _uv2s.Add(uv3);

        _uv2s.Add(uv2);
        _uv2s.Add(uv3);
        _uv2s.Add(uv4);
    }

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax) {
        _uv2s.Add(new Vector2(uMax, vMin));
        _uv2s.Add(new Vector2(uMin, vMin));
        _uv2s.Add(new Vector2(uMin, vMax));

        _uv2s.Add(new Vector2(uMax, vMin));
        _uv2s.Add(new Vector2(uMin, vMax));
        _uv2s.Add(new Vector2(uMax, vMax));
    }

    public void AddTriangleTerrainTypes(Vector3 types) {
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
    }

    public void AddQuadTerrainTypes(Vector3 types) {
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
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
