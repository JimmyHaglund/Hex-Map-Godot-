using System.IO;
using Godot;
using static System.Net.Mime.MediaTypeNames;

namespace JHM.MeshBasics;

public sealed partial class SaveLoadMenu : Control {
    private bool _saveMode;
    private const int _mapVersion = 1;

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

    public void PerformAction() {
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        if (_saveMode) {
            Save(path);
        }
        else {
            Load(path);
        }
        Close();
    }

    private string GetSelectedPath() {
        string mapName = NameInput.Text;
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(GetFilePath(mapName + ".map"));
    }

    private void Save(string filePath) {
        GD.Print(filePath);
        using var fileStream = File.Open(filePath, FileMode.Create);
        using var writer = new BinaryWriter(fileStream);
        writer.Write(_mapVersion);
        HexGrid.Save(writer);
    }

    private void Load(string filePath) {
        if (!File.Exists(filePath)) {
            GD.PrintErr($"File does not exist at path: {filePath}");
            return;
        }
        using var fileStream = File.OpenRead(filePath);
        using var reader = new BinaryReader(fileStream);
        int header = reader.ReadInt32();
        if (header > _mapVersion || header < 0) {
            GD.PrintErr($"Unknown map format {header}");
            return;
        }
        HexGrid.Load(reader, header);
    }

    private string GetFilePath(string fileName) => Path.Combine(OS.GetUserDataDir(), fileName);


}
