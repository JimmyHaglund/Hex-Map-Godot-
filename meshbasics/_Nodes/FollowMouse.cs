using Godot;

namespace JHM.MeshBasics; 

public sealed partial class FollowMouse : Node3D{
    public override void _Process(double delta) {
        Position = Mouse3D.MouseWorldPosition;
    }
}
