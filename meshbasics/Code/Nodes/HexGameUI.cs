using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGameUI : Control {
    private HexCell _currentCell;
    private HexUnit _selectedUnit;

    [Export] public HexGrid Grid { get; set; }
    
    public void SetEditMode(bool toggle) { 
        Visible = !toggle;
        ProcessMode = toggle ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        Grid.SetUIVisible(!toggle);
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) { 
            DoSelection();
        }
    }


    private bool UpdateCurrentCell() {
        var cell = Grid.GetCell(Mouse3D.MouseWorldPosition);
        if (cell == _currentCell) return false;
        _currentCell = cell;
        return true;
    }

    private void DoSelection() {
        UpdateCurrentCell();
        if (_currentCell is not null) {
            _selectedUnit = _currentCell.Unit;
        }
    }
}
