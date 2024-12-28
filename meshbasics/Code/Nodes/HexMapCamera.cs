using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapCamera : Node3D {
    private float _zoomedInPercentage = 0.5f;
    private Node3D _swivel;
    private Node3D _stick;

    [ExportCategory("Dependencies")]
    [Export] public HexGrid Grid { get; set; }

    [ExportCategory("Movement Settings")]
    [Export] public float ZoomedInMovementSpeed { get; set; } = 40.0f;
    [Export] public float ZoomedOutMovementSpeed { get; set; } = 400.0f;

    [ExportCategory("Zoom Settings")]
    [Export] public float ZoomFactor { get; set; } = 2.0f;
    [Export] public float StickZoomedInDistance { get; set; } = -45.0f;
    [Export] public float StickZoomedOutDistance { get; set; } = -250.0f;
    [Export] public float SwivelZoomedInAngle { get; set; } = 45.0f;
    [Export] public float SwivelZoomedOutAngle { get; set; } = 90.0f;

    public override void _EnterTree() {
        _swivel = GetChild<Node3D>(0);
        _stick = _swivel.GetChild<Node3D>(0);
    }

    public override void _Process(double delta) {
        var fDelta = (float)delta;
        float zoomDelta = fDelta * ZoomFactor * (
            Input.IsActionJustPressed("ZoomIn") ? 1.0f 
            : Input.IsActionJustPressed("ZoomOut") ? -1.0f 
            : 0.0f);
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }

        float inputMoveDeltaX = Input.IsActionPressed("MoveLeft") ? 1.0f
            : Input.IsActionPressed("MoveRight") ? -1.0f
            : 0.0f;
        float inputMoveDeltaY = Input.IsActionPressed("MoveForward") ? 1.0f
            : Input.IsActionPressed("MoveBack") ? -1.0f
            : 0.0f;
        if (inputMoveDeltaX != 0.0f || inputMoveDeltaY != 0.0f) {
            AdjustPosition(inputMoveDeltaX, inputMoveDeltaY, fDelta);
        }
        
    }

    private void AdjustZoom(float delta) {
        _zoomedInPercentage = Mathf.Clamp(_zoomedInPercentage + delta, 0.0f, 1.0f);

        var distance = Mathf.Lerp(StickZoomedOutDistance, StickZoomedInDistance, _zoomedInPercentage);
        _stick.Position = new(0.0f, 0.0f, distance);

        var angle = Mathf.DegToRad(Mathf.Lerp(SwivelZoomedOutAngle, SwivelZoomedInAngle, _zoomedInPercentage));
        var rot = _swivel.Rotation;
        rot.X = angle;
        _swivel.Rotation = rot;
    }

    private void AdjustPosition(float xDelta, float zDelta, float timeDelta) {
        Vector3 position = Position;
        Vector3 direction = new Vector3(xDelta, 0.0f, zDelta).Normalized();
        float distance = Mathf.Lerp(ZoomedOutMovementSpeed, ZoomedInMovementSpeed, _zoomedInPercentage) * timeDelta;
        position += direction * distance;
        Position = ClampPosition(position);
    }

    private Vector3 ClampPosition(Vector3 position) {
        float xMax =
            (Grid.ChunkCountX * HexMetrics.ChunkSizeX - 0.5f) *
            (2f * HexMetrics.InnerRadius);
        position.X = Mathf.Clamp(position.X, 0f, xMax);

        float zMax =
            (Grid.ChunkCountZ * HexMetrics.ChunkSizeZ - 1.0f)*
            (1.5f * HexMetrics.OuterRadius);
        position.Z = Mathf.Clamp(position.Z, 0f, zMax);

        return position;
    }
}
