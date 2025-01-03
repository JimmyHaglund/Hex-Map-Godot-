using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGameUI : Control {
    private HexCell _currentCell;
    private HexUnit _selectedUnit;

    [Export] public HexGrid Grid { get; set; }

    public override void _EnterTree() {
        HexGrid.MapReset += ClearSelection;
    }

    public override void _ExitTree() {
        HexGrid.MapReset -= ClearSelection;
    }


    private void ClearSelection() { 
        _selectedUnit = null;
        _currentCell = null;
    }

    public void SetEditMode(bool toggle) { 
        Visible = !toggle;
        ProcessMode = toggle ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        Grid.SetUIVisible(!toggle);
        Grid.ClearPath();
        HexCellShaderData.SetShaderParameter("HEX_MAP_EDIT_MODE", toggle);
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) { 
            DoSelection();
        }
        if (_selectedUnit is not null && Input.IsMouseButtonPressed(MouseButton.Right)) { 
            DoMove();
        }
    }

    public override void _Process(double delta) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) return;
        if (_selectedUnit is null) return;
        if (_currentCell is null) { 
            Grid.ClearPath();
            return;
        } 
        DoPathfinding();
    }

    private bool _pathing = false;
    private void DoPathfinding() {
        if (_pathing) return;
        if (!UpdateCurrentCell()) return;
        if (!_selectedUnit.IsValidDestination(_currentCell)) return;
        _pathing = true;
        Grid.FindPath(_selectedUnit.Location, _currentCell, 24);
        _pathing = false;
    }

    private bool UpdateCurrentCell() {
        var cell = Grid.GetCell(Mouse3D.MouseWorldPosition);
        if (cell == _currentCell) return false;
        _currentCell = cell;
        return true;
    }

    private void DoSelection() {
        Grid.ClearPath();
        UpdateCurrentCell();
        if (_currentCell is not null) {
            _selectedUnit = _currentCell.Unit;
        }
    }

    private void DoMove() {
        if (Grid.HasPath) {
            _selectedUnit.Travel(Grid.GetPath());
            Grid.ClearPath();
        }
    }
}
