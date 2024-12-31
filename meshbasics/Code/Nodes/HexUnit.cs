using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexUnit : Node3D{
    private HexCell _location;
    public HexCell Location {
        get {
            return _location;
        }
        set {
            _location = value;
            Position = value.Position;
        }
    }

}
