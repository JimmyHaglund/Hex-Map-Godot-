using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    private HexCell[] _neighbors = new HexCell[6];
    private int _elevation;
    private Node3D _uiRect;

    public Color Color { get; set; }
    public HexCoordinates Coordinates { get; set; }
    public Node3D UiRect {
        get => _uiRect;
        set {
            _uiRect = value;
            UpdateUiAltitude();
        }
    }
    public int Elevation {
        get => _elevation;
        set {
            _elevation = value;
            var newPosition = Position;
            newPosition.Y = value * HexMetrics.ElevationStep;
            newPosition.Y +=
                (HexMetrics.SampleNoise(newPosition).Y * 2f - 1f) *
                HexMetrics.ElevationPerturbStrength;
            Position = newPosition;
            UpdateUiAltitude();
        }
    }

    private void UpdateUiAltitude() {
        if (UiRect is null) return;
        var uiPosition = UiRect.Position;
        uiPosition.Y = Position.Y + 0.05f;
        UiRect.Position = uiPosition;
    }

    public HexCell GetNeighbor(HexDirection direction) {
        return _neighbors[(int)direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) {
        return HexMetrics.GetEdgeType(
            Elevation, _neighbors[(int)direction].Elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            Elevation, otherCell.Elevation
        );
    }
}