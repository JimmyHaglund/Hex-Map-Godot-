using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapCamera : Node3D {
    private float _zoomedInPercentage = 0.5f;
    private float _rotationAngle = 0.0f;
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

    [ExportCategory("Rotation Settings")]
    [Export] public float RotationSpeed { get; set; } = 5.0f;

    public bool Locked { get; set; } = false;

    public override void _EnterTree() {
        _swivel = GetChild<Node3D>(0);
        _stick = _swivel.GetChild<Node3D>(0);
    }

    public void SetLocked(bool value) => Locked = value;

    public override void _Process(double delta) {
        if (Locked) return;

        var fDeltaTime = (float)delta;
        float zoomDelta = fDeltaTime * ZoomFactor * (
            Input.IsActionJustPressed("ZoomIn") ? 1.0f 
            : Input.IsActionJustPressed("ZoomOut") ? -1.0f 
            : 0.0f);
        if (zoomDelta != 0f) {
            AdjustZoom(zoomDelta);
        }

        var inputRotation = Input.IsActionPressed("RotateLeft") ? 0.50f
            : Input.IsActionPressed("RotateRight") ? -0.50f
            : 0.0f;
        if (Input.IsActionPressed("EnableRotation")) {
            inputRotation += -0.0005f * Input.GetLastMouseVelocity().X;
        }
        if (inputRotation != 0.0f) {
            AdjustRotation(inputRotation, fDeltaTime);
        }

        float inputMoveDeltaX = Input.IsActionPressed("MoveLeft") ? 1.0f
            : Input.IsActionPressed("MoveRight") ? -1.0f
            : 0.0f;
        float inputMoveDeltaY = Input.IsActionPressed("MoveForward") ? 1.0f
            : Input.IsActionPressed("MoveBack") ? -1.0f
            : 0.0f;
        if (inputMoveDeltaX != 0.0f || inputMoveDeltaY != 0.0f) {
            AdjustPosition(inputMoveDeltaX, inputMoveDeltaY, fDeltaTime);
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
        Vector3 direction = Quaternion.FromEuler(Rotation) * new Vector3(xDelta, 0.0f, zDelta).Normalized();
        float distance = Mathf.Lerp(ZoomedOutMovementSpeed, ZoomedInMovementSpeed, _zoomedInPercentage) * timeDelta;
        position += direction * distance;
        Position = ClampPosition(position);
    }

    private Vector3 ClampPosition(Vector3 position) {
        float xMax =
            (Grid.CellCountX - 0.5f) *
            (2f * HexMetrics.InnerRadius);
        position.X = Mathf.Clamp(position.X, 0f, xMax);

        float zMax =
            (Grid.CellCountZ - 1.0f)*
            (1.5f * HexMetrics.OuterRadius);
        position.Z = Mathf.Clamp(position.Z, 0f, zMax);

        return position;
    }

    void AdjustRotation(float deltaRotation, float deltaTime) {
        _rotationAngle += deltaRotation * RotationSpeed * deltaTime;
        Rotation = new(0.0f, _rotationAngle, 0.0f);
    }
}
