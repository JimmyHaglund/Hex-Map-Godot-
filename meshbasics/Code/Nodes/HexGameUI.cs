using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGameUI : Control {
    [Export] public HexGrid Grid {get; set; }

    public void SetEditMode(bool toggle) { 
        Visible = !toggle;
        ProcessMode = toggle ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
        Grid.SetUIVisible(!toggle);
    }
}
