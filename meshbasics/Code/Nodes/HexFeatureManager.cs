using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexFeatureManager : Node3D {
    [Export] public PackedSceneContainer[] UrbanPrefabs { get; set; }
    [Export] public PackedSceneContainer[] FarmPrefabs { get; set; }
    [Export] public PackedSceneContainer[] PlantPrefabs { get; set; }

    private Node3D _container;

    private PackedScene PickPrefab(PackedSceneContainer[] collection, int level, float hash, float choice) {
        if (level > 0) {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++) {
                if (hash < thresholds[i]) {
                    return collection[i].Scenes[(int)(choice * collection[i].Scenes.Length)];
                }
            }
        }
        return null;
    }

    public void Clear() {
        if (_container != null) {
            _container.QueueFree();
        }
        _container = new Node3D();
        this.AddChild(_container);
        _container.Position = Position;
        _container.Name = "FeatureContainer";
    }

    public void Apply() { }

    public void AddFeature(HexCell cell, Vector3 position) {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        var prefab = PickPrefab(UrbanPrefabs, cell.UrbanLevel, hash.A, hash.D);
        var otherPrefab = PickPrefab(FarmPrefabs, cell.FarmLevel, hash.B, hash.D);
        var usedHash = hash.A;
        if (prefab != null && hash.B < hash.A) {
            prefab = otherPrefab;
            usedHash = hash.B;
        }
        else if (otherPrefab != null) {
            prefab = otherPrefab;
            usedHash = hash.B;
        }
        otherPrefab = PickPrefab(PlantPrefabs, cell.PlantLevel, hash.C, hash.D);
        if (prefab != null && hash.C < usedHash) {
            prefab = otherPrefab;
        }
        else if (otherPrefab != null) {
            prefab = otherPrefab;
        }
        if (prefab is null) return;

        var instance = _container.InstantiateChild<Node3D>(prefab);
        instance.Position = HexMetrics.Perturb(position);
        instance.Rotation = new(0.0f, 2 * Mathf.Pi * hash.E, 0.0f);
    }
}
