using Godot;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexFeatureManagerNode : Node3D {
    public HexFeatureManager HexFeatureManager {get; private set; }
    private Node3D _container;

    [Export] public PackedSceneContainer[] UrbanPrefabs { get; set; }
    [Export] public PackedSceneContainer[] FarmPrefabs { get; set; }
    [Export] public PackedSceneContainer[] PlantPrefabs { get; set; }
    [Export] public HexMeshNode Walls { get; set; }
    [Export] public PackedScene WallTower { get; set; }
    [Export] public PackedScene Bridge { get; set; }
    [Export] public PackedScene[] SpecialFeatures { get; set; }

    public void Apply() => HexFeatureManager.Apply();

    public void Clear() => HexFeatureManager.Clear();

    public void AddFeature(HexCell cell, Vector3 position) => HexFeatureManager.AddFeature(cell, position);

    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver, bool hasRoad) => HexFeatureManager.AddWall(near, nearCell, far, farCell, hasRiver, hasRoad);

    public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3) => HexFeatureManager.AddWall(c1, cell1, c2, cell2, c3, cell3);

    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2) => HexFeatureManager.AddBridge(roadCenter1, roadCenter2);

    public void AddSpecialFeature(HexCell cell, Vector3 position) => HexFeatureManager.AddSpecialFeature(cell, position);

    #region Lifetime

    public override void _EnterTree() {
        HexFeatureManager = new( 
            UrbanPrefabs.To2DArray(),
            FarmPrefabs.To2DArray(),
            PlantPrefabs.To2DArray(),
            Walls.HexMesh,
            WallTower,
            Bridge,
            SpecialFeatures
        );
    }

    #endregion
}
