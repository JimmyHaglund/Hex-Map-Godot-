﻿using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGameUI : Control {
    private HexCell _currentCell;
    private HexUnit _selectedUnit;

    [Export] public HexGrid Grid { get; set; }
    
    public void SetEditMode(bool toggle) { 
        Visible = !toggle;
        ProcessMode = toggle ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        Grid.SetUIVisible(!toggle);
        Grid.ClearPath();
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) { 
            DoSelection();
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


    private void DoPathfinding() {
        if (!UpdateCurrentCell()) return;
        Grid.FindPath(_selectedUnit.Location, _currentCell, 24);
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
}