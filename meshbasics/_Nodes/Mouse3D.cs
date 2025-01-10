using Godot;

namespace JHM;

public partial class Mouse3D : Node3D {
    [Export] public Camera3D Camera { get; set; }

    public static Vector3 MouseWorldPosition { get; private set; }

    public override void _PhysicsProcess(double delta) {
        MouseWorldPosition = Camera.GetMouseWorldPoint();
    }
}
