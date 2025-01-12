using Godot;

namespace JHM.HexaGrid;

public sealed class HexUnit {
    private HexCell? _location;
    private float _orientation;
    private float _moveProgress = 0.0f;
    private int _travelIndex = 0;
    private float _travelSpeed = 4.0f;

    public float TravelSpeed => _travelSpeed;
    public static PackedScene UnitPrefab { get; set; }
    public HexGrid? Grid { get; set; }

    public int VisionRange => 3;
    public List<HexCell>? PathToTravel {get; private set; }
    public Vector3 Position { get; private set; }

    public static Action<BinaryReader, HexGrid> LoadImplementation = (r, g) => { 
        throw new NotImplementedException("Loading and instantiating units must be handled by engine-side code.");
    };

    private HexCell? CurrentTravelLocation {
        get {
            if (PathToTravel is null) return _location;
            if (_travelIndex < 0 || _travelIndex >= PathToTravel.Count) return null;
            return PathToTravel[_travelIndex];
        }
    }


    public HexCell? Location {
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
        }
    }

    public float Orientation {
        get {
            return _orientation;
        }
        set {
            _orientation = value;
        }
    }

    public int Speed => 24;


    public static void Load(BinaryReader reader, HexGrid grid) {
        LoadImplementation.Invoke(reader, grid);
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
        }
        else if (fromCell.Walled != toCell.Walled) {
            return -1;
        }
        else {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }
        return moveCost;
    }

    public void Travel(List<HexCell> path) {
        Grid.DecreaseVisibility(CurrentTravelLocation, VisionRange);
        if (_location is not null) {
            _location.Unit = null;
        }
        _location = path[path.Count - 1];
        _location.Unit = this;

        PathToTravel = path;
        _travelIndex = 0;
        _moveProgress = 0.0f;
        Grid.IncreaseVisibility(CurrentTravelLocation, VisionRange);
    }

    public void Die() {
        Location = null;
    }

    public void Save(BinaryWriter writer) {
        var coordinates = Location?.Coordinates ?? new HexCoordinates();
        coordinates.Save(writer);
        writer.Write(Orientation);
    }

    public void TravelPath(float delta) {
        if (PathToTravel is null) return;
        _moveProgress += delta * _travelSpeed;
        int fromColumn = 0;
        int toColumn = 0;
        while (_moveProgress >= 1.0f) {
            if (_travelIndex + 1 < PathToTravel.Count) {
                Grid.DecreaseVisibility(CurrentTravelLocation, VisionRange);
            }
            fromColumn = toColumn = CurrentTravelLocation.ColumnIndex;
            _travelIndex++;
            _moveProgress--;
            if (CurrentTravelLocation != null) {
                toColumn = CurrentTravelLocation.ColumnIndex;
                Grid.MakeChildOfColumn(this, CurrentTravelLocation.ColumnIndex);
            }
            if (_travelIndex > 0) {
                Grid.IncreaseVisibility(CurrentTravelLocation, VisionRange);
            }
            if (_travelIndex + 1 >= PathToTravel.Count) break;
        }
        if (_travelIndex >= PathToTravel.Count) {
            PathToTravel = null;
            return;
        }

        Vector3 a, b, c;
        a = PathToTravel[0].Position;
        b = PathToTravel[_travelIndex].Position;
        c = b;

        if (_travelIndex > 0) {
            a = (PathToTravel[_travelIndex - 1].Position + PathToTravel[_travelIndex].Position) * 0.5f;
        }
        if (_travelIndex + 1 < PathToTravel.Count) {
            c = (b + PathToTravel[_travelIndex + 1].Position) * 0.5f;
        }

        if (toColumn < fromColumn - 1) {
            a.X -= HexMetrics.InnerDiameter * HexMetrics.wrapSize;
            b.X -= HexMetrics.InnerDiameter * HexMetrics.wrapSize;
        }
        else if (toColumn > fromColumn + 1) {
            a.X += HexMetrics.InnerDiameter * HexMetrics.wrapSize;
            b.X += HexMetrics.InnerDiameter * HexMetrics.wrapSize;
        }
        if (_travelIndex + 1 < PathToTravel.Count) {
            c = (b + PathToTravel[_travelIndex + 1].Position) * 0.5f;
        }

        Position = Bezier.GetPoint(a, b, c, _moveProgress);
    }

    public void Dispose() { 
        if (_location is null) return;
        _location.Unit = null;
    }
}
