using System.IO;
using System.Collections.Generic;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexUnit : Node3D {
    [Export] public PackedScene _pathDisplayPrefab;

    private HexCell _location;
    private float _orientation;
    private List<HexCell> _pathToTravel ;

    public static PackedScene UnitPrefab {get; set; }

    private List<Node3D> _pathDisplays = new();

    public HexCell Location {
        get {
            return _location;
        }
        set {
            if (_location is not null) {
                _location.Unit = null;
            }
            _location = value;
            value.Unit = this;
            Position = value.Position;
        }
    }

    public float Orientation {
        get {
            return _orientation;
        }
        set {
            _orientation = value;
            Rotation = new(0f, value, 0f);
        }
    }

    public void ValidateLocation() {
        Position = _location.Position;
    }

    public bool IsValidDestination(HexCell cell) {
        if (cell is null) return false;
        return !cell.IsUnderwater && cell.Unit is null;
    }

    public void Travel(List<HexCell> path) {
        _pathToTravel = path;
        ClearPathDisplay();
        _hasDrawnPath = false;
        // Location = path[path.Count - 1];
    }

    public void Die() { 
        _location.Unit = null;
        QueueFree();
    }

    public void Save(BinaryWriter writer) {
        Location.Coordinates.Save(writer);
        writer.Write(Orientation);
    }

    public static void Load(BinaryReader reader, HexGrid grid) {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();
        grid.AddUnit(
            grid.InstantiateOrphan<HexUnit>(UnitPrefab),
            grid.GetCell(coordinates),
            orientation
        );
    }

    
    private bool _hasDrawnPath = false;
    public override void _Process(double delta) {
        DrawPath();
    }

    private void DrawPath() {
        if (_pathToTravel == null || _pathToTravel.Count == 0 || _hasDrawnPath) {
            return;
        }
        
        ClearPathDisplay();

        for (var n = 0; n < _pathToTravel.Count; n++) { 
            var point = _pathToTravel[n];
            var node =   this.InstantiateChild<Node3D>(_pathDisplayPrefab);
            _pathDisplays.Add(node);
            node.GlobalPosition = point.Position;
            if (n + 1 < _pathToTravel.Count) { 
                node.LookAt(_pathToTravel[n + 1].Position);
            }
        }
        _hasDrawnPath = true;
    }

    private void ClearPathDisplay() {
        foreach (var n in _pathDisplays) n.QueueFree();
        _pathDisplays.Clear();
        _hasDrawnPath = false;
    }

    public override void _ExitTree() {
        if (_location is null) return;
        _location.Unit = null;
    }
}
