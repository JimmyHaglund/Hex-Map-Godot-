using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexMesh : MeshInstance3D {
    [Export] public CollisionShape3D CollisionShape { get; set; }

    private ArrayMesh _mesh;
    private List<Vector3> _vertices = new();
    // private List<int> _triangles = new();
    private List<Color> _colors = new();

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
        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }
        var surfaceTool = new SurfaceTool();

        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetMaterial(this.GetActiveMaterial(0));

        for(var n = _vertices.Count - 1; n >= 0; n--) {
            var vertex = _vertices[n];
            surfaceTool.SetColor(_colors[n]);
            surfaceTool.AddVertex(vertex);
        }

        surfaceTool.Commit(_mesh);

        CollisionShape.Shape = _mesh.CreateTrimeshShape();

        // _mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _vertices.ToArray());
        // hexMesh.triangles = triangles.ToArray();
        // hexMesh.RecalculateNormals();
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

        Vector3 v3 = center + HexMetrics.GetFirstCorner(direction);
        Vector3 v4 = center + HexMetrics.GetSecondCorner(direction);
        AddQuad(v1, v2, v3, v4);
        
        HexCell previousNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
        HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
        HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;
        AddQuadColor(
            cell.Color,
            cell.Color,
            (cell.Color + previousNeighbor.Color + neighbor.Color) / 3.0f,
            (cell.Color + neighbor.Color + nextNeighbor.Color) / 3.0f
        );

        // for (int i = 0; i < 6; i++) {
        //     AddTriangle(center, v1, v2);
        // 
        // 
        //     HexCell previousNeighbor = cell.GetNeighbor(direction.Previous()) ?? cell;
        //     HexCell neighbor = cell.GetNeighbor(direction) ?? cell;
        //     HexCell nextNeighbor = cell.GetNeighbor(direction.Next()) ?? cell;
        //     AddTriangleColor(
        //         cell.Color,
        //         (cell.Color + previousNeighbor.Color + neighbor.Color) / 3.0f,
        //         (cell.Color + neighbor.Color + nextNeighbor.Color) / 3.0f
        //     );
        // }
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        // int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
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
        // int vertexIndex = _vertices.Count;
        _vertices.Add(v3);
        _vertices.Add(v2);
        _vertices.Add(v1);

        _vertices.Add(v3);
        _vertices.Add(v4);
        _vertices.Add(v2);

        // triangles.Add(vertexIndex);
        // triangles.Add(vertexIndex + 2);
        // triangles.Add(vertexIndex + 1);
        // triangles.Add(vertexIndex + 1);
        // triangles.Add(vertexIndex + 2);
        // triangles.Add(vertexIndex + 3);
    }

    private void AddQuadColor(Color c1, Color c2, Color c3, Color c4) {
        _colors.Add(c3);
        _colors.Add(c2);
        _colors.Add(c1);

        _colors.Add(c3);
        _colors.Add(c4);
        _colors.Add(c2);
    }
}
