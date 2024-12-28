using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapCamera : Node3D {
    private float _zoom = 0.5f;
    private Node3D _swivel;
    private Node3D _stick;

    [Export] public float ZoomFactor { get; set; } = 2.0f;
    [Export] public float StickMaxZoom { get; set; } = -45.0f;
    [Export] public float StickMinZoom { get; set; } = -250.0f;
    [Export] public float SwivelMinZoom { get; set; } = 45.0f;
    [Export] public float SwivelMaxZoom { get; set; } = 90.0f;

    public override void _EnterTree() {
        _swivel = GetChild<Node3D>(0);
        _stick = _swivel.GetChild<Node3D>(0);
    }

    public override void _Process(double delta) {
        float zoomDelta = (float)delta * ZoomFactor * (
            Input.IsActionJustPressed("ZoomIn") ? 1.0f 
            : Input.IsActionJustPressed("ZoomOut") ? -1.0f 
            : 0.0f);
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }
    }

    private void AdjustZoom(float delta) {
        _zoom = Mathf.Clamp(_zoom + delta, 0.0f, 1.0f);

        var distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, _zoom);
        _stick.Position = new(0.0f, 0.0f, distance);

        var angle = Mathf.DegToRad(Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, _zoom));
        var rot = _swivel.Rotation;
        rot.X = angle;
        _swivel.Rotation = rot;
    }
}
