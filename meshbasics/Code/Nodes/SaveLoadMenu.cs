using Godot;

namespace JHM.MeshBasics;

public sealed partial class SaveLoadMenu : Control {
    private bool _saveMode;

    [Export] public HexGrid HexGrid { get; set; }
    [Export] public Label Title { get; set; }
    [Export] public Button ActionButton { get; set; }

    public void Open(bool saveMode) {
        _saveMode = saveMode;
        if (saveMode) {
            Title.Text = "Save Map";
            ActionButton.Text = "Save";
        } else {
            Title.Text = "Load Map";
            ActionButton.Text = "Load";
        }

        Visible = true;
        HexMapCamera.SetInstanceLocked(true);
    }

    public void Close() {
        Visible = false;
        HexMapCamera.SetInstanceLocked(false);
    }
}
