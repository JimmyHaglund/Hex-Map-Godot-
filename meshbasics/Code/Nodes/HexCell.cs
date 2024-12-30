using System.IO;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    private HexCell[] _neighbors = new HexCell[6];
    private int _elevation = int.MinValue;
    private Node3D _uiRect;
    private Color _color;
    private bool _hasIncomingRiver;
    private bool _hasOutgoingRiver;
    private bool _walled;
    private HexDirection _incomingRiver;
    private HexDirection _outgoingRiver;
    private bool[] _roads = new bool[6];
    private int _waterLevel;
    private int _urbanLevel;
    private int _farmLevel;
    private int _plantLevel;
    private int _specialIndex;
    private int _terrainTypeIndex;

    public HexCoordinates Coordinates { get; set; }
    public HexGridChunk Chunk { get; set; }

    public Node3D UiRect {
        get => _uiRect;
        set {
            _uiRect = value;
            UpdateUiAltitude();
        }
    }

    public int Elevation {
        get => _elevation;
        set {
            if (_elevation == value) return;
            _elevation = value;
            RefreshPosition();
            ValidateRivers();

            for (int i = 0; i < _roads.Length; i++) {
                if (_roads[i] && GetElevationDifference((HexDirection)i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public int TerrainTypeIndex {
        get {
            return _terrainTypeIndex;
        }
        set {
            if (_terrainTypeIndex != value) {
                _terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public Color Color => HexMetrics.Colors[_terrainTypeIndex];

    public bool HasIncomingRiver {
        get {
            return _hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver {
        get {
            return _hasOutgoingRiver;
        }
    }

    public bool Walled {
        get {
            return _walled;
        }
        set {
            if (_walled == value) return;
            _walled = value;
            Refresh();
        }
    }

    public HexDirection IncomingRiver {
        get {
            return _incomingRiver;
        }
    }

    public HexDirection OutgoingRiver {
        get {
            return _outgoingRiver;
        }
    }

    public bool HasRiver {
        get {
            return _hasIncomingRiver || _hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd {
        get {
            return _hasIncomingRiver != _hasOutgoingRiver;
        }
    }

    public float StreamBedY {
        get {
            return
                (Elevation + HexMetrics.StreamBedElevationOffset) *
                HexMetrics.ElevationStep;
        }
    }

    public float RiverSurfaceY {
        get {
            return
                (Elevation + HexMetrics.WaterElevationOffset) *
                HexMetrics.ElevationStep;
        }
    }

    public float WaterSurfaceY {
        get {
            return
                (_waterLevel + HexMetrics.WaterElevationOffset) *
                HexMetrics.ElevationStep;
        }
    }

    public bool HasRoads {
        get {
            for (int i = 0; i < _roads.Length; i++) {
                if (_roads[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    public HexDirection RiverBeginOrEndDirection {
        get {
            return HasIncomingRiver ? IncomingRiver : OutgoingRiver;
        }
    }

    public int WaterLevel {
        get {
            return _waterLevel;
        }
        set {
            if (_waterLevel == value) {
                return;
            }
            _waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater {
        get {
            return _waterLevel > _elevation;
        }
    }

    public int UrbanLevel {
        get {
            return _urbanLevel;
        }
        set {
            if (_urbanLevel != value) {
                _urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel {
        get {
            return _farmLevel;
        }
        set {
            if (_farmLevel != value) {
                _farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel {
        get {
            return _plantLevel;
        }
        set {
            if (_plantLevel != value) {
                _plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int SpecialIndex {
        get {
            return _specialIndex;
        }
        set {
            if (_specialIndex != value && !HasRiver) {
                _specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial {
        get {
            return _specialIndex > 0;
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction) {
        return
            _hasIncomingRiver && _incomingRiver == direction ||
            _hasOutgoingRiver && _outgoingRiver == direction;
    }

    public bool HasRoadThroughEdge(HexDirection direction) {
        return _roads[(int)direction];
    }

    public void SetShowLabel(bool visible) {
        if (UiRect is null) return;
        UiRect.Visible = visible;
    }


    private bool IsValidRiverDestination(HexCell neighbor) {
        if (neighbor is null) return false;
        return Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation;
    }

    private void UpdateUiAltitude() {
        if (UiRect is null) return;
        var uiPosition = UiRect.Position;
        uiPosition.Y = Position.Y + 0.05f;
        UiRect.Position = uiPosition;
    }

    public HexCell GetNeighbor(HexDirection direction) {
        return _neighbors[(int)direction];
    }

    public int GetElevationDifference(HexDirection direction) {
        int difference = Elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) {
        return HexMetrics.GetEdgeType(
            Elevation, _neighbors[(int)direction].Elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            Elevation, otherCell.Elevation
        );
    }

    public void RemoveOutgoingRiver() {
        if (!_hasOutgoingRiver) {
            return;
        }
        _hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_outgoingRiver);
        neighbor._hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver() {
        if (!_hasIncomingRiver) {
            return;
        }
        _hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_incomingRiver);
        neighbor._hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver() {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void AddRoad(HexDirection direction) {
        if (!_roads[(int)direction] 
            && !HasRiverThroughEdge(direction)
            && !IsSpecial 
            && !GetNeighbor(direction).IsSpecial
            && GetElevationDifference(direction) <= 1
        ) {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoads() {
        for (int i = 0; i < _neighbors.Length; i++) {
            if (_roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    public void RefreshPosition() {
        var newPosition = Position;
        newPosition.Y = _elevation * HexMetrics.ElevationStep;
        newPosition.Y +=
            (HexMetrics.SampleNoise(newPosition).Y * 2f - 1f) *
            HexMetrics.ElevationPerturbStrength;
        Position = newPosition;
        UpdateUiAltitude();
    }

    public void Save(BinaryWriter writer) {
        writer.Write((byte)_terrainTypeIndex);
        writer.Write((byte)_elevation);
        writer.Write((byte)_waterLevel);
        writer.Write((byte)_urbanLevel);
        writer.Write((byte)_farmLevel);
        writer.Write((byte)_plantLevel);
        writer.Write((byte)_specialIndex);
        writer.Write(_walled);
        if (_hasIncomingRiver) {
            // Use the eight bit to store the bool _hasIncomingRiver. The 8th bit corresponds to 128.
            writer.Write((byte)(_incomingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        if (_hasOutgoingRiver) {
            writer.Write((byte)(_outgoingRiver + 128));
        }
        else {
            writer.Write((byte)0);
        }

        for (int i = 0; i < _roads.Length; i++) {
            writer.Write(_roads[i]);
        }
    }

    public void Load(BinaryReader reader) {
        _terrainTypeIndex = reader.ReadByte();
        _elevation = reader.ReadByte();
        _waterLevel = reader.ReadByte();
        _urbanLevel = reader.ReadByte();
        _farmLevel = reader.ReadByte();
        _plantLevel = reader.ReadByte();
        _specialIndex = reader.ReadByte();
        _walled = reader.ReadBoolean();

        byte riverData = reader.ReadByte();
        if (riverData >= 128) {
            _hasIncomingRiver = true;
            _incomingRiver = (HexDirection)(riverData - 128);
        }
        else {
            _hasIncomingRiver = false;
        }
        riverData = reader.ReadByte();
        if (riverData >= 128) {
            _hasOutgoingRiver = true;
            _outgoingRiver = (HexDirection)(riverData - 128);
        }
        else {
            _hasOutgoingRiver = false;
        }
        
        for (int i = 0; i < _roads.Length; i++) {
            _roads[i] = reader.ReadBoolean();
        }

        RefreshPosition();
    }

    private void SetRoad(int index, bool state) {
        _roads[index] = state;
        _neighbors[index]._roads[(int)((HexDirection)index).Opposite()] = state;
        _neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    private void ValidateRivers() {
        if (
            HasOutgoingRiver &&
            !IsValidRiverDestination(GetNeighbor(OutgoingRiver))
        ) {
            RemoveOutgoingRiver();
        }
        if (
            HasIncomingRiver &&
            !GetNeighbor(IncomingRiver).IsValidRiverDestination(this)
        ) {
            RemoveIncomingRiver();
        }
    }

    private void Refresh() {
        if (Chunk is null) return;
        Chunk.Refresh();

        for (int i = 0; i < _neighbors.Length; i++) {
            HexCell neighbor = _neighbors[i];
            if (neighbor is null || neighbor.Chunk == Chunk) continue;
            neighbor.Chunk.Refresh();
        }
    }

    private void RefreshSelfOnly() {
        Chunk.Refresh();
    }

    public void SetOutgoingRiver(HexDirection direction) {
        if (_hasOutgoingRiver && _outgoingRiver == direction) {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor)) {
            return;
        }

        RemoveOutgoingRiver();
        if (_hasIncomingRiver && _incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        _hasOutgoingRiver = true;
        _outgoingRiver = direction;
        _specialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();
        neighbor._specialIndex = 0;

        SetRoad((int)direction, false);
    }


}