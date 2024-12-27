using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    public Color Color { get; set; }
    public HexCoordinates Coordinates { get; set; }
}