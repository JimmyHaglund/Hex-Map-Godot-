using Godot;

namespace JHM.MeshBasics;
public sealed partial class SaveLoadItem : Button {
    private string _mapName;

    public SaveLoadMenu Menu { get; set; }

    public string MapName {
        get {
            return _mapName;
        }
        set {
            _mapName = value;
            Text = value;
        }
    }

    public void Select() {
        Menu.SelectItem(_mapName);
    }
}