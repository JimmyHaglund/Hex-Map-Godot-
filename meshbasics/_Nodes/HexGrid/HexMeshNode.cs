using Godot;
using System.Collections.Generic;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexMeshNode : MeshInstance3D {
    public HexMesh HexMesh { get; set; }
    [Export] public CollisionShape3D CollisionShape { get; set; }
    [Export] public CollisionShape3D AltShape { get; set; }
    [Export] public bool UseCollider { get; set; } = true;
    [Export] public bool UseCellData { get; set; }
    [Export] public bool UseUVCoordinates { get; set; } = false;
    [Export] public bool UseUV2Coordinates { get; set; } = false;
    
    public override void _Ready() {
        Initialise();
    }

    public void Initialise() {
        if (HexMesh is not null) return;
        var mesh = Mesh as ArrayMesh;
        if (Mesh is null) {
            GD.PrintErr("HexMesh requires an ArrayMesh.");
            return;
        }
        HexMesh = new HexMesh(
            GetActiveMaterial(0),
            mesh, CollisionShape,
            AltShape,
            UseCollider,
            UseCellData,
            UseUVCoordinates,
            UseUV2Coordinates
        );
        HexMesh.ApplyCompleted += () => CallDeferred("SwapCollisionShape");
    }

    public void Apply() => HexMesh.Apply();

    public void Clear() => HexMesh.Clear();

    public void SetVertices(List<Vector3> vertices) => HexMesh.SetVertices(vertices);

    public void SetNormals(List<Vector3> normals) => HexMesh.SetNormals(normals);

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) => HexMesh.AddTriangle(v1, v2, v3);

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3) => HexMesh.AddTriangleUnperturbed(v1, v2, v3);

    public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3) => HexMesh.AddTriangleCellData(indices, weights1, weights2, weights3);

    public void AddTriangleCellData(Vector3 indices, Color weights) => HexMesh.AddTriangleCellData(indices, weights);

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) => HexMesh.AddQuad(v1, v2, v3, v4);

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4) => HexMesh.AddQuadUnperturbed(v1, v2, v3, v4); 

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4) => HexMesh.AddQuadCellData(indices, weights1, weights2, weights3, weights4);

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2) => HexMesh.AddQuadCellData(indices, weights1, weights2);

    public void AddQuadCellData(Vector3 indices, Color weights) => HexMesh.AddQuadCellData(indices, weights);

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3) => HexMesh.AddTriangleUV(uv1, uv2, uv3);

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => HexMesh.AddQuadUV(uv1, uv2, uv3, uv4);

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax) => HexMesh.AddQuadUV(uMin, uMax, vMin, vMax);

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3) => HexMesh.AddTriangleUV2(uv1, uv2, uv3);

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4) => HexMesh.AddQuadUV2(uv1, uv2, uv3, uv4);

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax) => HexMesh.AddQuadUV2(uMin, uMax, vMin, vMax);

    private void SwapCollisionShape() => HexMesh.SwapCollisionShape();
}
