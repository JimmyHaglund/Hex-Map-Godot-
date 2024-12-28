using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapEditor : Control {
    [Export]
    public Color[] Colors { get; set; }
    [Export] public HexGrid HexGrid { get; set; }

    private Color _activeColor;
    private int _activeElevation = 1;
    private bool _mouseIsDown = false;
    private bool _applyColor = false;
    private bool _applyElevation = true;
    private int _brushSize;
    private OptionalToggle _riverMode;

    // public override void _PhysicsProcess(double _) {
    //     if (Input.IsMouseButtonPressed(MouseButton.Left)) {
    //         if (_mouseIsDown) return;
    //         HandleInput();
    //     } else {
    //         _mouseIsDown = false;
    //     }
    // }

    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            if (_mouseIsDown) return;
            HandleInput();
        }
        else {
            _mouseIsDown = false;
        }
    }

    void HandleInput() {
        if (HexGrid.IsRefreshing) return;
        var mousePosition = Mouse3D.MouseWorldPosition;
        EditCells(HexGrid.GetCell(mousePosition));
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
        // HexGrid.Refresh();
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

    #region Definitions
    private enum OptionalToggle {
        Disable = 0, On = 1, Off = 2
    }
    #endregion
}
