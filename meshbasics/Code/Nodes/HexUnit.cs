using System.IO;
using Godot;
using static Godot.TextServer;

namespace JHM.MeshBasics;

public sealed partial class HexUnit : Node3D{
    private HexCell _location;
    private float _orientation;
    public static PackedScene UnitPrefab;

    public HexCell Location {
        get {
            return _location;
        }
        set {
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

    public override void _ExitTree() {
        if (_location is null) return;
        _location.Unit = null;
    }
}
