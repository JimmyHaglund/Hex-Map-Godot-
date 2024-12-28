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

        var shape = _mesh.CreateTrimeshShape();
        _inactiveShape.Shape = shape;
        CallDeferred("SwapCollisionShape");
    }

    private void Triangulate(HexCell cell) {
        for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++) {
            Triangulate(direction, cell);
        }
    }

    private void SwapCollisionShape() {
        var deactivated = _activeShape;
        var activated = _inactiveShape;
        _activeShape = activated;
        _inactiveShape = deactivated;
        activated.ProcessMode = ProcessModeEnum.Inherit;
        deactivated.ProcessMode = ProcessModeEnum.Disabled;
    }

    private void Triangulate(HexDirection direction, HexCell cell) {
        Vector3 center = cell.Position;
        EdgeVertices e = new(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        TriangulateEdgeFan(center, e, cell.Color);

        // Vector3 v1 = center + HexMetrics.GetFirstSolidCorner(direction);
        // Vector3 v2 = center + HexMetrics.GetSecondSolidCorner(direction);
        // 
        // Vector3 e1 = v1.Lerp(v2, 1.0f / 3.0f);
        // Vector3 e2 = v1.Lerp(v2, 2.0f / 3.0f);
        // 
        // AddTriangle(center, v1, e1);
        // AddTriangleColor(cell.Color);
        // AddTriangle(center, e1, e2);
        // AddTriangleColor(cell.Color);
        // AddTriangle(center, e2, v2);
        // AddTriangleColor(cell.Color);
        // 
        // var triangulateConnection = () => TriangulateConnection(direction, cell, v1, e1, e2, v2);
        // if (direction == HexDirection.NE) {
        //     triangulateConnection();
        // }
        
        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, e);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1) {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor is null) return;
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.Y = neighbor.Position.Y - cell.Position.Y;
        EdgeVertices e2 = new(e1.v1 + bridge, e1.v4 + bridge);

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) { 
            TriangulateEdgeTerraces(e1, cell, e2, neighbor);
        } else {
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor is not null) {
            Vector3 v5 = e1.v4 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.Position.Y;

            if (cell.Elevation <= neighbor.Elevation) {
                if (cell.Elevation <= nextNeighbor.Elevation) {
                    // If the cell is the lowest (or tied for lowest) of its neighbors, use it as the bottom one.
                    TriangulateCorner(e1.v4, cell, e2.v4, neighbor, v5, nextNeighbor);
                } else {
                    // If nextNeighbor is lowest...
                    TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
                }
            } else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                TriangulateCorner(e2.v4, neighbor, v5, nextNeighbor, e1.v4, cell);
            } else {
                TriangulateCorner(v5, nextNeighbor, e1.v4, cell, e2.v4, neighbor);
            }

            // AddTriangle(v2, v4, v5);
            // AddTriangleColor(cell.Color, neighbor.Color, nextNeighbor.Color);
        }
    }

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell
    ) {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, c2);
    }

    private void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) {
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            } else if (rightEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            } else {  
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        } else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        } else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            if (leftCell.Elevation < rightCell.Elevation) {
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            } else {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        } else { 
            AddTriangle(bottom, left, right);
            AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);
        
        AddTriangle(begin, v3, v4);
        AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            AddQuad(v1, v2, v3, v4);
            AddQuadColor(c1, c2, c3, c4);
        }

        AddQuad(v3, v4, left, right);
        AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        float b = 1.0f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = Perturb(begin).Lerp(Perturb(right), b);
        Color boundaryColor = beginCell.Color.Lerp(rightCell.Color, b);

        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        float b = 1.0f / (leftCell.Elevation - beginCell.Elevation);
        b *= Mathf.Sign(b);
        Vector3 boundary = Perturb(begin).Lerp(Perturb(left), b);
        Color boundaryColor = beginCell.Color.Lerp(leftCell.Color, b);

        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else {
            AddTriangleUnperturbed(Perturb(left), Perturb(right), boundary);
            AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }


    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    ) {
        Vector3 v2 = Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);


        AddTriangleUnperturbed(Perturb(begin), v2, boundary);
        AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            AddTriangleUnperturbed(v1, v2, boundary);
            AddTriangleColor(c1, c2, boundaryColor);
        }

        AddTriangleUnperturbed(v2, Perturb(left), boundary);
        AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color) {
        AddTriangle(center, edge.v1, edge.v2);
        AddTriangleColor(color);
        AddTriangle(center, edge.v2, edge.v3);
        AddTriangleColor(color);
        AddTriangle(center, edge.v3, edge.v4);
        AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2
    ) {
        AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        AddQuadColor(c1, c2);
        AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        AddQuadColor(c1, c2);
        AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        AddQuadColor(c1, c2);
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        //int vertexIndex = _vertices.Count;
        var normal = (v2 - v1).Cross(v3 - v2);
        _vertices.Add(Perturb(v1));
        _vertices.Add(Perturb(v2));
        _vertices.Add(Perturb(v3));
        _normals.Add(normal);
        _normals.Add(normal);
        _normals.Add(normal);

        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 2);
    }

    private void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3) {
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
        //int vertexIndex = _vertices.Count;
        var normal = (v2 - v1).Cross(v2 - v3);
        _vertices.Add(Perturb(v2));
        _vertices.Add(Perturb(v1));
        _vertices.Add(Perturb(v3));

        _vertices.Add(Perturb(v2));
        _vertices.Add(Perturb(v3));
        _vertices.Add(Perturb(v4));

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

    private Vector3 Perturb(Vector3 position) {
        Vector4 sample = HexMetrics.SampleNoise(position);
        position.X += HexMetrics.CellPerturbStrength * (2.0f * sample.X - 1.0f);
        // position.Y += HexMetrics.CellPerturbStrength * (2.0f * sample.Y - 1.0f);
        position.Z += HexMetrics.CellPerturbStrength * (2.0f * sample.Z - 1.0f);
        return position;
    }
}
