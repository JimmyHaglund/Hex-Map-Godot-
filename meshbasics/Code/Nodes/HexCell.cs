using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    private HexCell[] _neighbors = new HexCell[6];
    private int _elevation;

    public Color Color { get; set; }
    public HexCoordinates Coordinates { get; set; }
    public int Elevation {
        get => _elevation;
        set {
            _elevation = value;
            var newPosition = Position;
            newPosition.Y = value * HexMetrics.ElevationStep;
            Position = newPosition;
        }
    }

    public HexCell GetNeighbor(HexDirection direction) {
        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }
}