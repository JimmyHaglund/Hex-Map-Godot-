using Godot;

namespace JHM.MeshBasics {
    public sealed partial class PackedSceneContainer : Node {
        [Export] public PackedScene[] Scenes { get; set; }
    }
}
