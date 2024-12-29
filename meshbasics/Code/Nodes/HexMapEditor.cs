using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapEditor : Control {
    [Export]
    public Color[] Colors { get; set; }
    [Export] public HexGrid HexGrid { get; set; }

    private Color _activeColor;
    private int _activeElevation = 1;
    private bool _applyColor = false;
    private bool _applyElevation = true;
    private int _brushSize;
    private OptionalToggle _riverMode;
    private OptionalToggle _roadMode;
    private bool _isDrag;
    private HexDirection _dragDirection;
    private HexCell _previousCell;

    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            HandleInput();
        }
        else {
            _previousCell = null;
        }
    }

    void HandleInput() {
        if (HexGrid.IsRefreshing) return;
        var mousePosition = Mouse3D.MouseWorldPosition;
        var cell = HexGrid.GetCell(mousePosition);
        if (cell is null) {
            _previousCell = null;
            return;
        }
        if (_previousCell != null && _previousCell != cell) {
            ValidateDrag(cell);
        } else {
            _isDrag = false;
        }

        EditCells(cell);
        _previousCell = cell;
    }

    private void ValidateDrag(HexCell currentCell) {
        for (
            _dragDirection = HexDirection.NE;
            _dragDirection <= HexDirection.NW;
            _dragDirection++
        ) {
            if (_previousCell.GetNeighbor(_dragDirection) == currentCell) {
                _isDrag = true;
                return;
            }
        }
        _isDrag = false;
    }

    private void EditCells(HexCell center) {
        if (center is null) return;
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;
        for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++) {
            for (int x = centerX - r; x <= centerX + _brushSize; x++) {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++) {
            for (int x = centerX - _brushSize; x <= centerX + r; x++) {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

    }

    private void EditCell(HexCell cell) {
        if (cell is null) return;
        if (_applyColor) {
            cell.Color = _activeColor;
        }
        if (_applyElevation) {
            cell.Elevation = _activeElevation;
        }

        if (_riverMode == OptionalToggle.Off) {
            cell.RemoveRiver();
        }
        if (_roadMode == OptionalToggle.Off) {
            cell.RemoveRoads();
        }
        if (_isDrag) {
            HexCell otherCell = cell.GetNeighbor(_dragDirection.Opposite());
            if (otherCell is not null) { 
                if (_riverMode == OptionalToggle.On) {
                    otherCell.SetOutgoingRiver(_dragDirection);
                }
                if (_roadMode == OptionalToggle.On) { 
                    otherCell.AddRoad(_dragDirection);
                }
            }
        }
    }

    public void SetElevation(float elevationStep) {
        // var result = HexMetrics.Maxelevation * elevationStep / 100;
        _activeElevation = (int)elevationStep;
    }

    public void SelectColor(int index) {
        if (index < 0 || index >= Colors.Length) {
            _applyColor = false;
            return;
        }
        _activeColor = Colors[index];
        _applyColor = true;
    }

    public void SetColor(Color color) {
        _activeColor = color;
    }

    public void SetApplyColor(bool value) => _applyColor = value;

    public void ToggleElevationEnabled(bool value) {
        _applyElevation = value;
    }

    public void SetBrushSize(float size) {
        _brushSize = (int)size;
    }

    public void SetRiverMode(int mode) {
        _riverMode = (OptionalToggle)mode;
    }

    public void SetRoadMode(int mode) {
        _roadMode = (OptionalToggle)mode;
    }

    #region Definitions
    private enum OptionalToggle {
        Disable = 0, On = 1, Off = 2
    }
    #endregion
}
