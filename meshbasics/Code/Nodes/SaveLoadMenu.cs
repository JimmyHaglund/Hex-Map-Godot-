using System;
using System.IO;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class SaveLoadMenu : Control {
    [Export] private Node _listContentParent;
    [Export] private PackedScene _saveLoadItemPrefab { get; set; }
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

        FillFileList();
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

    public void SelectItem(string name) {
        NameInput.Text = name;
    }

    private string GetSelectedPath() {
        string mapName = NameInput.Text;
        if (mapName.Length == 0) {
            return null;
        }
        return Path.Combine(GetFilePath(mapName + ".map"));
    }

    public void Save(string filePath) {
        GD.Print(filePath);
        using var fileStream = File.Open(filePath, FileMode.Create);
        using var writer = new BinaryWriter(fileStream);
        writer.Write(_mapVersion);
        HexGrid.Save(writer);
    }

    public void Load(string filePath) {
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

    public void Delete() {
        string path = GetSelectedPath();
        if (path == null) {
            return;
        }
        if (File.Exists(path)) return;
        File.Delete(path);
        NameInput.Text = "";
        FillFileList();
    }

    private void FillFileList() {
        string[] paths = Directory.GetFiles(OS.GetUserDataDir(), "*.map");
        Array.Sort(paths);

        for (int i = 0; i < _listContentParent.GetChildCount(); i++) {
            _listContentParent.GetChild(i).QueueFree();
        }

        for (int i = 0; i < paths.Length; i++) {
            SaveLoadItem item = _listContentParent.InstantiateChild<SaveLoadItem>(_saveLoadItemPrefab);
            item.Menu = this;
            item.MapName = Path.GetFileNameWithoutExtension(paths[i]);
        }
    }

    private string GetFilePath(string fileName) => Path.Combine(OS.GetUserDataDir(), fileName);
}
