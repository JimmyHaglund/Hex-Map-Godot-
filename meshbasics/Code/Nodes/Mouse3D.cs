using Godot;

namespace JHM;

public partial class Mouse3D : Node3D {
    [Export] public Camera3D Camera { get; set; }

    public static Vector3 MouseWorldPosition { get; private set; }

    public override void _PhysicsProcess(double delta) {
        MouseWorldPosition = GetMouseWorldPoint();
    }

    private Vector3 GetMouseWorldPoint() {
        var mouseScreenPosition = Camera.GetViewport().GetMousePosition();
        var rayOrigin = Camera.ProjectRayOrigin(mouseScreenPosition);
        var rayDirection = Camera.ProjectRayNormal(mouseScreenPosition);
        var spaceState = GetWorld3D().DirectSpaceState;
        var rayEnd = rayOrigin + rayDirection * Camera.Far;
        var raycastParameters = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        var rayResult = spaceState.IntersectRay(raycastParameters);
        if (rayResult.Count == 0) return rayEnd;
        return rayResult["position"].AsVector3();
    }
}
