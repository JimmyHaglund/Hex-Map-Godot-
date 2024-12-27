using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    public Color Color { get; set; }
    public HexCoordinates Coordinates { get; set; }
    private HexCell[] _neighbors = new HexCell[6];

    public HexCell GetNeighbor(HexDirection direction) {
        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }
}