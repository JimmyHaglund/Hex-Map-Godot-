using System.IO;
using Godot;
using static System.Net.Mime.MediaTypeNames;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    private HexCell[] _neighbors = new HexCell[6];
    private int _elevation = int.MinValue;
    private Label3D _label;
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
    private int _distance;
    private int _visibility;
    private bool _isExplored = false;

    public HexCoordinates Coordinates { get; set; }
    public HexGridChunk Chunk { get; set; }
    public HexCell PathFrom { get; set; }
    public int SearchHeuristic { get; set; }
    public int SearchPhase { get; set; }
    public HexCellShaderData ShaderData { get; set; }
    public int Index { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    public HexUnit Unit { get; set; }
    public bool Explorable { get; set; }

    public bool IsExplored { 
        get {
            return _isExplored && Explorable;
        } 
        private set { 
            _isExplored = value;
        } 
    }

    public Label3D Label {
        get => _label;
        set {
            _label = value;
            UpdateUiAltitude();
        }
    }

    public int Elevation {
        get => _elevation;
        set {
            if (_elevation == value) return;
            int originalViewElevation = ViewElevation;
            _elevation = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
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
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    public int Distance {
        get {
            return _distance;
        }
        set {
            _distance = value;
        }
    }

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
            int originalViewElevation = ViewElevation;
            _waterLevel = value;
            if (ViewElevation != originalViewElevation) {
                ShaderData.ViewElevationChanged();
            }
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

    public int SearchPriority {
        get {
            return _distance + SearchHeuristic;
        }
    }

    public new bool IsVisible {
        get {
            return _visibility > 0 && Explorable;
        }
    }

    public int ViewElevation {
        get {
            return _elevation >= _waterLevel ? _elevation : _waterLevel;
        }
    }

    public void IncreaseVisibility() {
        _visibility += 1;
        if (_visibility == 1) {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility() {
        _visibility -= 1;
        if (_visibility == 0) { 
            ShaderData.RefreshVisibility(this);
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
        if (Label is null) return;
        Label.Visible = visible;
    }

    public void SetMapData(float data) {
        ShaderData.SetMapData(this, data);
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexCell GetNeighbor(HexDirection direction) {
        return _neighbors[(int)direction];
    }

    public int GetElevationDifference(HexDirection direction) {
        int difference = Elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
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

    public void ResetVisibility() {
        if (_visibility > 0) {
            _visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }

    

    public void Save(BinaryWriter writer) {
        writer.Write((byte)_terrainTypeIndex);
        writer.Write((byte)_elevation + 127);
        writer.Write((byte)_waterLevel);
        writer.Write((byte)_urbanLevel);
        writer.Write((byte)_farmLevel);
        writer.Write((byte)_plantLevel);
        writer.Write((byte)_specialIndex);
        writer.Write(_walled);

        // Use the eight bit to store the bool _hasIncomingRiver. The 8th bit corresponds to 128.
        if (_hasIncomingRiver) {
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

        int roadFlags = 0;
        for (int i = 0; i < _roads.Length; i++) {
            if (_roads[i]) {
                roadFlags |= 1 << i;
            }
        }
        writer.Write((byte)roadFlags);

        writer.Write(IsExplored);
    }

    public void Load(BinaryReader reader, int header) {
        _terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
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

        int roadFlags = reader.ReadByte();
        for (int i = 0; i < _roads.Length; i++) {
            _roads[i] = (roadFlags & (1 << i)) != 0;
        }

        RefreshPosition();

        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshVisibility(this);
        if (header >= 4) _elevation += 127;

    }

    


    private bool IsValidRiverDestination(HexCell neighbor) {
        if (neighbor is null) return false;
        return Elevation >= neighbor.Elevation || WaterLevel == neighbor.Elevation;
    }

    private void UpdateUiAltitude() {
        if (Label is null) return;
        var uiPosition = Label.Position;
        uiPosition.Y = Position.Y + 0.05f;
        Label.Position = uiPosition;
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

        if (Unit is not null) { 
            Unit.ValidateLocation();
        }
    }

    private void RefreshSelfOnly() {
        Chunk.Refresh();

        if (Unit is not null) {
            Unit.ValidateLocation();
        }
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

    public void EnableHighlight(Color color) {
        SetHighlightVisible(true, color);
    }

    public void DisableHighlight() {
        SetHighlightVisible(false, new(1,1,1,1));
    }

    public void SetLabel(string text) {
        _label.Text = text;
    }

    private void SetHighlightVisible(bool visible, Color color) {
        if (IsQueuedForDeletion()) return;
        if (Label is null) return;
        if (Label.IsQueuedForDeletion()) return;
        var highlight = Label.GetChild<Sprite3D>(0);
        highlight.Modulate = color;
        highlight.Visible = visible;
    }
}