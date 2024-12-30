using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexFeatureManager : Node3D {
    [Export] public PackedScene FeaturePrefab { get; set; }

    public void Clear() { }

    public void Apply() { }

    public void AddFeature(Vector3 position) {
        var instance = this.InstantiateChild<Node3D>(FeaturePrefab);
        instance.Position = HexMetrics.Perturb(position);
    }
}
