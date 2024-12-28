using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexMesh : MeshInstance3D {
    [Export] public CollisionShape3D CollisionShape { get; set; }

    private ArrayMesh _mesh;
    private List<Vector3> _vertices = new();
    // private List<int> _triangles = new();
    private List<Color> _colors = new();
    private List<Vector3> _normals = new();

    public override void _Ready() {
        _mesh = Mesh as ArrayMesh;
        if (_mesh is null) {
            GD.PrintErr("HexMesh requires an ArrayMesh.");
        }
    }

    public void Triangulate(HexCell[] cells) {
        _mesh.ClearSurfaces();
        _vertices.Clear();
        // _triangles.Clear();
        _colors.Clear();
        _normals.Clear();
        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }
        var surfaceTool = new SurfaceTool();

        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetMaterial(this.GetActiveMaterial(0));

        for(var n = _vertices.Count - 1; n >= 0; n--) {
            var vertex = _vertices[n];
            surfaceTool.SetNormal(_normals[n]);
            surfaceTool.SetColor(_colors[n]);
            surfaceTool.AddVertex(vertex);
        }
        surfaceTool.Commit(_mesh);

        CollisionShape.Shape = _mesh.CreateTrimeshShape();
    }

    private void Triangulate(HexCell cell) {
        for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++) {
            Triangulate(direction, cell);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell) {
        Vector3 center = cell.Position;
        Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);

        AddTriangle(center, v1, v2);
        AddTriangleColor(cell.Color);
        var triangulateConnection = () => TriangulateConnection(direction, cell, v1, v2);
        
        if (direction == HexDirection.NE) {
            triangulateConnection();
        }
        
        if (direction <= HexDirection.SE) {
            triangulateConnection();
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, Vector3 v1, Vector3 v2) {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor is null) return;
        Vector3 bridge = HexMetrics.GetBridge(direction);
        Vector3 v3 = v1 + bridge;
        Vector3 v4 = v2 + bridge;
        v3.Y = v4.Y = neighbor.Elevation * HexMetrics.ElevationStep;

        TriangulateEdgeTerraces(v1, v2, cell, v3, v4, neighbor);
        // AddQuad(v1, v2, v3, v4);
        // AddQuadColor(cell.Color, neighbor.Color);

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor is not null) {
            Vector3 v5 = v2 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.Elevation * HexMetrics.ElevationStep;
            AddTriangle(v2, v4, v5);
            AddTriangleColor(cell.Color, neighbor.Color, nextNeighbor.Color);
        }
    }

    private void TriangulateEdgeTerraces(
        Vector3 beginLeft,
        Vector3 beginRight,
        HexCell beginCell,
        Vector3 endLeft,
        Vector3 endRight,
        HexCell endCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(beginRight, endRight, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        AddQuad(beginLeft, beginRight, v3, v4);
        AddQuadColor(beginCell.Color, c2);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c2;
            v3 = HexMetrics.TerraceLerp(beginLeft, endLeft, i);
            v4 = HexMetrics.TerraceLerp(beginRight, endRight, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2);
        }


        AddQuad(v3, v4, endLeft, endRight);
        AddQuadColor(c2, endCell.Color);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = _vertices.Count;
        var normal = (v2 - v1).Cross(v3 - v2);
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 2);
    }

    private void AddTriangleColor(Color color) {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }

    private void AddTriangleColor(Color c1, Color c2, Color c3) {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);

    }

    private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) {
        int vertexIndex = _vertices.Count;
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
        // _vertices.Add(v4);

        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 2);

        // _triangles.Add(vertexIndex + 2);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 3);

    }

    private void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c3);

        _colors.Add(c2);
        _colors.Add(c3);
        _colors.Add(c4);
    }

    private void AddQuadColor(Color c1, Color c2) {
        _colors.Add(c1);
        _colors.Add(c1);
        _colors.Add(c2);

        _colors.Add(c1);
        _colors.Add(c2);
        _colors.Add(c2);
    }
}
