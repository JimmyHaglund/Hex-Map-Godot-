using System.IO;
using System.Collections.Generic;
using Godot;
using static Godot.TextServer;

namespace JHM.MeshBasics;

public sealed partial class HexUnit : Node3D {
    [Export] public PackedScene _pathDisplayPrefab;

    private float _travelSpeed = 4.0f;
    private HexCell _location;
    private float _orientation;
    private List<HexCell> _pathToTravel ;
    private float _moveProgress = 0.0f;
    private int _travelIndex = 0;
    private float _rotationSpeed = Mathf.Pi;
    private float _rotationTarget;

    public int VisionRange => 3;

    public static PackedScene UnitPrefab {get; set; }
    public HexGrid Grid { get; set; }
    private HexCell CurrentTravelLocation {
        get {
            if (_pathToTravel is null) return _location;
            if (_travelIndex < 0 || _travelIndex >= _pathToTravel.Count) return null;
            return _pathToTravel[_travelIndex];
        }
    }

    private List<Node3D> _pathDisplays = new();
    
    public HexCell Location {
        get {
            return _location;
        }
        set {
            if (_location is not null) {
                Grid?.DecreaseVisibility(_location, VisionRange);
                _location.Unit = null;
            }
            if (value is null) return;

            _location = value;
            _location.Unit = this;
            Grid?.IncreaseVisibility(_location, VisionRange);
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

    public int Speed => 24;

    public void ValidateLocation() {
        Position = _location.Position;
    }

    public bool IsValidDestination(HexCell cell) {
        if (cell is null) return false;
        return cell.IsExplored && !cell.IsUnderwater && cell.Unit is null;
    }

    public int GetMoveCost(
        HexCell fromCell,
        HexCell toCell,
        HexDirection direction
    ) {
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff) {
            return -1;
        }
        int moveCost;
        if (fromCell.HasRoadThroughEdge(direction)) {
            moveCost = 1;
        } else if (fromCell.Walled != toCell.Walled) {
            return -1;
        } else {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }
        return moveCost;
    }

    public void Travel(List<HexCell> path) {
        Grid.DecreaseVisibility(CurrentTravelLocation, VisionRange);
        _location.Unit = null;

        _location = path[path.Count - 1];
        _location.Unit = this;

        _pathToTravel = path;
        _travelIndex = 0;
        _moveProgress = 0.0f;
        Grid.IncreaseVisibility(CurrentTravelLocation, VisionRange);

        ClearPathDisplay();
    }

    public void Die() {
        Location = null;
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

        Vector3 a, b, c = _pathToTravel[0].Position;

        for (int i = 1; i < _pathToTravel.Count; i++) {
            a = c;
            b = _pathToTravel[i - 1].Position;
            c = (b + _pathToTravel[i].Position) * 0.5f;
            for (float t = 0f; t < 1f; t += 0.05f * _travelSpeed) {
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
        b = _pathToTravel[_pathToTravel.Count - 1].Position;
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
    private void TravelPath(float delta) {

        if (_pathToTravel is null) return;
        _moveProgress += delta * _travelSpeed;
        while (_moveProgress >= 1.0f) {
            if (_travelIndex + 1 < _pathToTravel.Count) {
                Grid.DecreaseVisibility(CurrentTravelLocation, VisionRange);
            }
            _travelIndex++;
            _moveProgress--;
            if (_travelIndex > 0) {
                Grid.IncreaseVisibility(CurrentTravelLocation, VisionRange);
            }
            if (_travelIndex + 1 >= _pathToTravel.Count) break;
        }
        if (_travelIndex >= _pathToTravel.Count) {
            _pathToTravel = null;
            return;
        }

        Vector3 a, b, c;
        a = _pathToTravel[0].Position;
        b = _pathToTravel[_travelIndex].Position;
        c = b;

        if (_travelIndex > 0) { 
            a = (_pathToTravel[_travelIndex - 1].Position + _pathToTravel[_travelIndex].Position) * 0.5f;
        
        }
        if (_travelIndex + 1 < _pathToTravel.Count) {
            c = (b + _pathToTravel[_travelIndex + 1].Position) * 0.5f;
        }
        Position = Bezier.GetPoint(a, b, c, _moveProgress);

        if (_travelIndex + 1 < _pathToTravel.Count) { 
            var lookDirection = Bezier.GetDerivative(a, b, c, _moveProgress);
            lookDirection.Y = 0;
            LookAt(Position + lookDirection);
            Orientation = Rotation.Y;
        }
    }

    public override void _ExitTree() {
        ClearPathDisplay();
        if (_location is null) return;
        _location.Unit = null;
    }

    //private void LookAt(Vector3 point) {
    //    point.Y = Position.Y;
    //    
    //    Orientation = Rotation.Y;
    //}

    private void LookAtInterpolated(Vector3 point, float deltaTime) { 
        var deltaAngle = deltaTime * _rotationSpeed;
        var targetRotation = point - Position;
        var rotationDifference = Rotation.SignedAngleTo(targetRotation, Vector3.Up);
        if (rotationDifference == 0.0f) return;
        deltaAngle *= Mathf.Sign(rotationDifference) / Mathf.Abs(rotationDifference);

        if (deltaAngle < 0) {
            deltaAngle = Mathf.Max(deltaAngle, rotationDifference);
        } else { 
            deltaAngle = Mathf.Min(deltaAngle, rotationDifference);
        }

        Rotate(Vector3.Up, deltaAngle);
    }
}
