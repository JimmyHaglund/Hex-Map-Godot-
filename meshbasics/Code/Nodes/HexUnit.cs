using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexUnit : Node3D{
    private HexCell _location;
    private float _orientation;

    public HexCell Location {
        get {
            return _location;
        }
        set {
            _location = value;
            value.Unit = this;
            Position = value.Position;
        }
    }

    public float Orientation {
        get {
            return _orientation;
        }
        set {
            _orientation = value;
            Rotation = new(0f, value, 0f);
        }
    }

    public void ValidateLocation() {
        Position = _location.Position;
    }
    public void Die() { 
        _location.Unit = null;
        QueueFree();
    }

    public override void _ExitTree() {
        if (_location is null) return;
        _location.Unit = null;
    }
}
