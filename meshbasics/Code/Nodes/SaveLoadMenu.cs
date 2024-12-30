using Godot;

namespace JHM.MeshBasics;

public sealed partial class SaveLoadMenu : Control {
    private bool _saveMode;

    [Export] public HexGrid HexGrid { get; set; }

    public void Open(bool saveMode) {
        _saveMode = saveMode;
        Visible = true;
        HexMapCamera.SetInstanceLocked(true);

    }

    public void Close() {
        Visible = false;
        HexMapCamera.SetInstanceLocked(false);
    }
}
