using Godot;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexCellShaderDataNode : Node {
    private HexCellShaderData _shaderData = new();

    public bool ImmediateMode { 
        get => _shaderData.ImmediateMode; 
        set => _shaderData.ImmediateMode = value;
    }

    public HexGrid Grid {
        get => _shaderData.Grid;
        set => _shaderData.Grid = value;
    }

    public void SetMapData(HexCell cell, float data) => _shaderData.SetMapData(cell, data);

    public void Initialize(int x, int z) => _shaderData.Initialize(x, z);

    public void RefreshTerrain(HexCell cell) => _shaderData.RefreshTerrain(cell);

    public void RefreshVisibility(HexCell cell) => _shaderData.RefreshVisibility(cell);

    public void ViewElevationChanged() => _shaderData.ViewElevationChanged();

    public override void _EnterTree() {
        _shaderData.ActiveSet += active => ProcessMode = active ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        // ProcessMode = ProcessModeEnum.Disabled;
    }

    public override void _Process(double delta) {
        CallDeferred("LateUpdate", (float)delta);
    }

    private void LateUpdate(float deltaTime) => _shaderData.Update(deltaTime);
}
