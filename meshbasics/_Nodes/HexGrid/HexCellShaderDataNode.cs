using Godot;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexCellShaderDataNode : Node {
    private HexCellShaderData _shaderData;

    public bool ImmediateMode { 
        get => _shaderData.ImmediateMode; 
        set => _shaderData.ImmediateMode = value;
    }

    public HexGrid Grid {
        get => _shaderData.Grid;
        set => _shaderData.Grid = value;
    }

    public void SetMapData(HexCell cell, float data) {
        _shaderData.SetMapData(cell, data);
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void Initialize(int x, int z) {
        _shaderData.Initialize(x, z);
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void RefreshTerrain(HexCell cell) {
        _shaderData.RefreshTerrain(cell);
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void RefreshVisibility(HexCell cell) {
        _shaderData.RefreshVisibility(cell);
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void ViewElevationChanged() {
        _shaderData.ViewElevationChanged();
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public override void _EnterTree() {
        _shaderData = new();
        ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta) {
        CallDeferred("LateUpdate", (float)delta);
    }

    private void LateUpdate(float deltaTime) {
        _shaderData.Update(deltaTime);
        if (!_shaderData.HasTransitioningCells) { 
            ProcessMode = ProcessModeEnum.Disabled;
        }
    }
}
