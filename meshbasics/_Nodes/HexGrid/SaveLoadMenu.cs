using System;
using System.IO;
using Godot;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class SaveLoadMenu : Control {
    [Export] private Node _listContentParent;
    [Export] private PackedScene _saveLoadItemPrefab { get; set; }
    private bool _saveMode;
    private const int _mapVersion = 5;

    [Export] public HexGrid.HexGrid HexGrid { get; set; }
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
        BinarySaveLoad.Save(filePath, w => { 
            w.Write(_mapVersion);
            HexGrid.Save(w);
        });
    }

    public void Load(string filePath) {
        BinarySaveLoad.Load(filePath, r => {
            int header = r.ReadInt32();
            if (header > _mapVersion || header < 0) {
                GD.PrintErr($"Unknown map format {header}");
                return;
            }
            HexGrid.Load(r, header);
        });
    }

    public void Delete() {
        string path = GetSelectedPath();
        if (string.IsNullOrEmpty(path)) {
            return;
        }
        BinarySaveLoad.Delete(path);
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
