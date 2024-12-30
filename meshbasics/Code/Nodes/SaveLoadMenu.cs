using System.IO;
using Godot;
using static System.Net.Mime.MediaTypeNames;

namespace JHM.MeshBasics;

public sealed partial class SaveLoadMenu : Control {
    private bool _saveMode;

    [Export] public HexGrid HexGrid { get; set; }
    [Export] public Label Title { get; set; }
    [Export] public Button ActionButton { get; set; }
    [Export] public TextEdit NameInput { get; set; }

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

    private string GetSelectedPath() {
        string mapName = NameInput.Text;
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(GetFilePath(mapName + ".map"));
    }

    private string GetFilePath(string fileName) => Path.Combine(OS.GetUserDataDir(), fileName);
}
