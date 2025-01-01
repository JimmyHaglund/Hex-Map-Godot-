using System.IO;
using System.Collections.Generic;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexUnit : Node3D {
    [Export] public PackedScene _pathDisplayPrefab;

    private float _travelSpeed = 4.0f;
    private HexCell _location;
    private float _orientation;
    private List<HexCell> _pathToTravel ;
    private float _moveProgress = 0.0f;
    private int _travelIndex = 0;

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
        Location = path[path.Count - 1];
        _pathToTravel = path;
        _travelIndex = 0;
        _moveProgress = 0.0f;

        ClearPathDisplay();
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
        TravelPath((float)delta);
        DrawPath();
    }

    private void DrawPath() {
        if (_pathToTravel == null || _pathToTravel.Count == 0 || _hasDrawnPath) {
            return;
        }
        
        ClearPathDisplay();

        Vector3 a, b = _pathToTravel[0].Position;

        for (var n = 0; n + 1 < _pathToTravel.Count; n++) { 
            var point = _pathToTravel[n];
            var node = GetTree().Root.GetChild(0).InstantiateChild<Node3D>(_pathDisplayPrefab);
            a = b;
            b = (_pathToTravel[n].Position + _pathToTravel[n + 1].Position) * 0.5f;
            _pathDisplays.Add(node);
            node.GlobalPosition = a;
            node.LookAt(b);
        }
        _hasDrawnPath = true;
    }

    private void ClearPathDisplay() {
        foreach (var n in _pathDisplays) n.QueueFree();
        _pathDisplays.Clear();
        _hasDrawnPath = false;
    }

    private void TravelPath(float delta) {
        if (_pathToTravel is null) return;
        _moveProgress += delta * _travelSpeed;
        while (_moveProgress >= 1.0f) { 
            _travelIndex++;
            _moveProgress--;
        }
        if (_travelIndex >= _pathToTravel.Count) return;

        Vector3 a, b;
        a = _pathToTravel[0].Position;
        b = _pathToTravel[_travelIndex].Position;

        if (_travelIndex > 0) { 
            a = (_pathToTravel[_travelIndex - 1].Position + _pathToTravel[_travelIndex].Position) * 0.5f;
        
        }
        if (_travelIndex < _pathToTravel.Count - 1) {
            b = (_pathToTravel[_travelIndex].Position + _pathToTravel[_travelIndex + 1].Position) * 0.5f;
        }
        Position = a.Lerp(b, _moveProgress);
        LookAt(Position + b - a);
    }

    public override void _ExitTree() {
        if (_location is null) return;
        _location.Unit = null;
    }
}
