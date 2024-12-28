using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapCamera : Node3D {
    public Node3D Stick { get; set; }
    public Node3D Swivel { get; set; }

    public override void _EnterTree() {
        Swivel = GetChild<Node3D>(0);
        Stick = Swivel.GetChild<Node3D>(0);
    }
}
