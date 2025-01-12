using System.IO;
using System.Collections.Generic;
using Godot;
using JHM.HexaGrid;
using static Godot.TextServer;

namespace JHM.MeshBasics;

public sealed partial class HexUnitNode : Node3D {
    [Export] private PackedScene _pathDisplayPrefab;
    private bool _hasDrawnPath = false;
    private List<Node3D> _pathDisplays = new();

    public HexUnit Unit { get; } = new();
    public int VisionRange => Unit.VisionRange;
    public HexGrid Grid { 
        get => Unit.Grid;
        set => Unit.Grid = value;
    }
    public HexCell Location {
        get => Unit.Location;
        set { 
            Unit.Location = value;
            if (value is null) return;
            Position = value.Position;
        }
    }
    public float Orientation {
        get => Unit.Orientation;
        set { 
            Unit.Orientation = value;
            Rotation = new(0.0f, value, 0.0f);
        }
    }
    public List<HexCell> PathToTravel => Unit.PathToTravel;
    public int Speed => Unit.Speed;
    private float TravelSpeed => Unit.TravelSpeed;

    public void ValidateLocation() {
        Position = Unit.Position;
    }

    public bool IsValidDestination(HexCell cell) => Unit.IsValidDestination(cell);

    public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction) {
        return Unit.GetMoveCost(fromCell, toCell, direction);
    }

    public void Travel(List<HexCell> path) {
        Unit.Travel(path);
        ClearPathDisplay();
    }

    public void Die() {
        Unit.Die();
        QueueFree();
    }

    public void Save(BinaryWriter writer) {
        Unit.Save(writer);
    }

    public static void Load(BinaryReader reader, HexGridNode grid) {
        HexUnit.LoadImplementation = LoadImplementation;
        HexUnit.Load(reader, grid.Grid);
    }

    private static void LoadImplementation(BinaryReader reader, HexGrid grid) {
        HexCoordinates coordinates = HexCoordinates.Load(reader);
        float orientation = reader.ReadSingle();

        var unit = SceneInstantiator.InstantiateOrphan<HexUnitNode>(HexUnit.UnitPrefab);
        
        grid.AddUnit(
            unit.Unit,
            grid.GetCell(coordinates),
            orientation
        );
        grid.HexGridRoot.AddChild(unit);
    }
    
    public override void _Process(double delta) {
        var fDelta = (float)delta;
        Unit.TravelPath(fDelta);
        Position = Unit.Position;
        DrawPath();
    }

    private void DrawPath() {
        if (Unit.PathToTravel == null || PathToTravel.Count == 0 || _hasDrawnPath) {
            return;
        }
        ClearPathDisplay();

        Vector3 a, b, c = PathToTravel[0].Position;

        for (int i = 1; i < PathToTravel.Count; i++) {
            a = c;
            b = PathToTravel[i - 1].Position;
            c = (b + PathToTravel[i].Position) * 0.5f;
            for (float t = 0f; t < 1f; t += 0.05f * Unit.TravelSpeed) {
                var node = GetTree().Root.GetChild(0).InstantiateChild<Node3D>(_pathDisplayPrefab);
                var pos = Bezier.GetPoint(a, b, c, t);
                _pathDisplays.Add(node);
                node.GlobalPosition = pos;
                var derivative = Bezier.GetDerivative(a, b, c, t);
                if (!derivative.IsEqualApprox(Vector3.Zero)) {
                    node.LookAt(node.Position + derivative);
                }
            }
        }

        a = c;
        b = PathToTravel[PathToTravel.Count - 1].Position;
        c = b;
        for (float t = 0f; t < 1f; t += 0.1f) {
            var node = GetTree().Root.GetChild(0).InstantiateChild<Node3D>(_pathDisplayPrefab);
            var pos = Bezier.GetPoint(a, b, c, t);
            _pathDisplays.Add(node);
            node.GlobalPosition = pos;
            node.LookAt(node.Position + Bezier.GetDerivative(a,b,c,t));
        }
        _hasDrawnPath = true;
    }

    private void ClearPathDisplay() {
        foreach (var n in _pathDisplays) n.QueueFree();
        _pathDisplays.Clear();
        _hasDrawnPath = false;
    }

    private bool _lookBeforeMoving = false;

    public override void _ExitTree() {
        ClearPathDisplay();
        Unit.Dispose();
    }
}
