using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    public HexCoordinates Coordinates { get; set; }
}