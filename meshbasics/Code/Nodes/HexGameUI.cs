using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGameUI : Control {
    private HexCell _currentCell;

    [Export] public HexGrid Grid { get; set; }
    
    public void SetEditMode(bool toggle) { 
        Visible = !toggle;
        ProcessMode = toggle ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        Grid.SetUIVisible(!toggle);
    }

    private bool UpdateCurrentCell() {
        var cell = Grid.GetCell(Mouse3D.MouseWorldPosition);
        if (cell == _currentCell) return false;
        _currentCell = cell;
        return true;
    }
}
