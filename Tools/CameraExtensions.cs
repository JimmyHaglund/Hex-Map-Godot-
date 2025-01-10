using Godot;

namespace JHM; 
public static class CameraExtensions {
    public static Vector3 GetMouseWorldPoint(this Camera3D camera) {
        var mouseScreenPosition = camera.GetViewport().GetMousePosition();
        var rayOrigin = camera.ProjectRayOrigin(mouseScreenPosition);
        var rayDirection = camera.ProjectRayNormal(mouseScreenPosition);
        var spaceState = camera.GetWorld3D().DirectSpaceState;
        var rayEnd = rayOrigin + rayDirection * camera.Far;
        var raycastParameters = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
        var rayResult = spaceState.IntersectRay(raycastParameters);
        if (rayResult.Count == 0) return rayEnd;
        return rayResult["position"].AsVector3();
    }
}
