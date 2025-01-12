using Godot;
using System.Collections.Generic;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexMeshNode : MeshInstance3D {
    private HexMesh _mesh;
    [Export] public CollisionShape3D CollisionShape { get; set; }
    [Export] public CollisionShape3D AltShape { get; set; }
    [Export] public bool UseCollider { get; set; } = true;
    [Export] public bool UseCellData { get; set; }
    [Export] public bool UseUVCoordinates { get; set; } = false;
    [Export] public bool UseUV2Coordinates { get; set; } = false;
    
    public override void _Ready() {
        var mesh = Mesh as ArrayMesh;
        if (_mesh is null) {
            GD.PrintErr("HexMesh requires an ArrayMesh.");
            return;
        }
        _mesh = new HexMesh(mesh, CollisionShape, AltShape, UseCollider, UseCellData, UseUVCoordinates, UseUV2Coordinates);
    }

    public void Apply() {
        _mesh.Apply(this.GetActiveMaterial(0));
        CallDeferred("SwapCollisionShape");
    }

    public void Clear() => _mesh.Clear();

    public void SetVertices(List<Vector3> vertices) => _mesh.SetVertices(vertices);

    public void SetNormals(List<Vector3> normals) => _mesh.SetNormals(normals);

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) => _mesh.AddTriangle(v1, v2, v3);

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3) => _mesh.AddTriangleUnperturbed(v1, v2, v3);

    public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3) => _mesh.AddTriangleCellData(indices, weights1, weights2, weights3);

    public void AddTriangleCellData(Vector3 indices, Color weights) => _mesh.AddTriangleCellData(indices, weights);

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) => _mesh.AddQuad(v1, v2, v3, v4);

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) => _mesh.AddQuadUnperturbed(v1, v2, v3, v4); 

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4) => _mesh.AddQuadCellData(indices, weights1, weights2, weights3, weights4);

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2) => _mesh.AddQuadCellData(indices, weights1, weights2);

    public void AddQuadCellData(Vector3 indices, Color weights) => _mesh.AddQuadCellData(indices, weights);

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3) => _mesh.AddTriangleUV(uv1, uv2, uv3);

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => _mesh.AddQuadUV(uv1, uv2, uv3, uv4);

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax) => _mesh.AddQuadUV(uMin, uMax, vMin, vMax);

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3) => _mesh.AddTriangleUV2(uv1, uv2, uv3);

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => _mesh.AddQuadUV2(uv1, uv2, uv3, uv4);

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax) => _mesh.AddQuadUV2(uMin, uMax, vMin, vMax);
}
