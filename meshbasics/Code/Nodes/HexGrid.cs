using System.IO;
using System;
using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexGrid : Node3D {
    // private HexMesh _hexMesh;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;
    private int _chunkCountZ;
    private int _chunkCountX;

    [ExportCategory("HexGrid Dependencies")]
    [Export] private PackedScene _hexUnitPrefab;
    [Export] public int CellCountX { get; set; } = 20;
    [Export] public int CellCountZ { get; set; } = 15;
    [Export] public PackedScene HexCellPrefab { get; set; }
    [Export] public PackedScene CellLabelPrefab { get; set; }
    [Export] public Texture2D NoiseSource { get; set; }
    [Export] public PackedScene ChunkPrefab { get; set; }

    [ExportCategory("HexGrid Configuration")]
    [Export] public int Seed { get; set; } = 1234;
    
    private int _refreshStack = 0;
    public bool IsRefreshing => _refreshStack > 0;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;
    private HexCell _currentPathFrom;
    private HexCell _currentPathTo;
    private bool _currentPathExists;
    private List<HexUnit> _units = new List<HexUnit>();
    [Export] private HexCellShaderData _cellShaderData;

    public static event Action MapReset;

    public bool HasPath {
        get {
            return _currentPathExists;
        }
    }

    public override void _EnterTree() {
        HexMetrics.NoiseSource = NoiseSource.GetImage();
        HexMetrics.InitializeHashGrid(Seed);

        // _cellShaderData = new();
        // AddChild(_cellShaderData);
        CreateMap(CellCountX, CellCountZ);
    }

    public HexCell GetCell(Vector3 position) {
        position = ClampPositionToGrid(position);
        var coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
        if (index >= _cells.Length || index < 0) return null;
        return _cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates) {
        int z = coordinates.Z;
        if (z < 0 || z >= CellCountZ) {
            return null;
        }
        int x = coordinates.X + z / 2;
        if (x < 0 || x >= CellCountX) {
            return null;
        }

        return _cells[x + z * CellCountX];
    }

    public void SetUIVisible(bool visible) {
        foreach (var chunk in _chunks) chunk.SetUIVisible(visible);
    }

    public bool CreateMap(int cellCountX, int cellCountZ) {
        if (
            cellCountX <= 0 || cellCountX % HexMetrics.ChunkSizeX != 0 ||
            cellCountZ <= 0 || cellCountZ % HexMetrics.ChunkSizeZ != 0
        ) {
            GD.PrintErr("Unsupported map size.");
            return false;
        }
        ClearPath();
        ClearUnits();
        if (_chunks != null) {
            for (int i = 0; i < _chunks.Length; i++) {
                _chunks[i].QueueFree();
            }
        }

        CellCountX = cellCountX;
        CellCountZ = cellCountZ;

        _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        _cellShaderData.Initialize(cellCountX, cellCountZ);
        CreateChunks();
        CreateCells();
        HexMapCamera.ValidatePosition();
        MapReset?.Invoke();
        return true;
    }

    public void Save(BinaryWriter writer) {
        writer.Write(CellCountX);
        writer.Write(CellCountZ);
        for (int i = 0; i < _cells.Length; i++) {
            _cells[i].Save(writer);
        }

        writer.Write(_units.Count);
        for (int i = 0; i < _units.Count; i++) {
            _units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header) {
        ClearPath();
        ClearUnits();
        int x = 20, z = 15;
        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }
        if (x != CellCountX && z != CellCountZ) { 
            if (!CreateMap(x, z)) {
                return;
            }
        }
        for (int i = 0; i < _cells.Length; i++) {
            _cells[i].Load(reader, header);
        }
        for (int i = 0; i < _chunks.Length; i++) {
            _chunks[i].Refresh();
        }

        if (header < 2) return;
        int unitCount = reader.ReadInt32();
        for (int i = 0; i < unitCount; i++) {
            HexUnit.Load(reader, this);
        }
    }

    public void FindPath(HexCell fromCell, HexCell toCell, int speed) {
        ClearPath();
        _currentPathFrom = fromCell;
        _currentPathTo = toCell;
        if (toCell is null) return;
        _currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation) {
        _units.Add(unit);
        AddChild(unit);
        unit.Grid = this;
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit) {
        _units.Remove(unit);
        unit.Die();
    }

    public void ClearPath() {
        if (_currentPathExists) {
            HexCell current = _currentPathTo;
            while (current != _currentPathFrom) {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }
            current.DisableHighlight();
            _currentPathExists = false;
        }
        _currentPathFrom = _currentPathTo = null;
    }

    public List<HexCell> GetPath() {
        if (!_currentPathExists) {
            return null;
        }
        List<HexCell> path = ListPool<HexCell>.Get();
        for (HexCell c = _currentPathTo; c != _currentPathFrom; c = c.PathFrom) {
            path.Add(c);
        }
        path.Add(_currentPathFrom);
        path.Reverse();
        return path;
    }

    public void IncreaseVisibility(HexCell fromCell, int range) {
        if (fromCell is null) return;
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++) {
            cells[i].IncreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    public void DecreaseVisibility(HexCell fromCell, int range) {
        if (fromCell is null) return;
        List<HexCell> cells = GetVisibleCells(fromCell, range);
        for (int i = 0; i < cells.Count; i++) {
            cells[i].DecreaseVisibility();
        }
        ListPool<HexCell>.Add(cells);
    }

    private Vector3 ClampPositionToGrid(Vector3 position) {
        float xMax =
            (CellCountX - 0.5f) *
            (2f * HexMetrics.InnerRadius);
        position.X = Mathf.Clamp(position.X, 0f, xMax);

        float zMax =
            (CellCountZ - 1.0f) *
            (1.5f * HexMetrics.OuterRadius);
        position.Z = Mathf.Clamp(position.Z, 0f, zMax);

        return position;
    }

    private bool Search(HexCell fromCell, HexCell toCell, int speed) {
        _searchFrontierPhase += 2;
        if (_searchFrontier == null) {
            _searchFrontier = new HexCellPriorityQueue();
        }
        _searchFrontier.Clear();
        
        
        fromCell.Distance = 0;
        fromCell.SearchPhase = _searchFrontierPhase;
        _searchFrontier.Enqueue(fromCell);
        while (_searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell) {
                return true;
            }

            int currentTurn = (current.Distance - 1) / speed;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > _searchFrontierPhase) {
                    continue;
                }
                if (neighbor.IsUnderwater || neighbor.Unit != null) {
                    continue;
                }
                HexEdgeType edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff) {
                    continue;
                }
                int moveCost;
                if (current.HasRoadThroughEdge(d)) {
                    moveCost = 1;
                }
                else if (current.Walled != neighbor.Walled) {
                    continue;
                }
                else {
                    moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel +
                        neighbor.PlantLevel;
                }

                int distance = current.Distance + moveCost;
                int turn = (distance - 1) / speed;
                if (turn > currentTurn) {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                    _searchFrontier.Enqueue(neighbor);
                } else if (distance < neighbor.Distance) {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.PathFrom = current;
                    neighbor.Distance = distance;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return false;
    }

    private List<HexCell> GetVisibleCells(HexCell fromCell, int range) {
        List<HexCell> visibleCells = ListPool<HexCell>.Get();
        _searchFrontierPhase += 2;
        if (_searchFrontier == null) {
            _searchFrontier = new HexCellPriorityQueue();
        }
        _searchFrontier.Clear();


        fromCell.Distance = 0;
        fromCell.SearchPhase = _searchFrontierPhase;
        _searchFrontier.Enqueue(fromCell);
        while (_searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            current.SearchPhase += 1;

            visibleCells.Add(current);

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor == null || neighbor.SearchPhase > _searchFrontierPhase) {
                    continue;
                }

                int distance = current.Distance + 1;
                if (distance > range) continue;

                if (neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.SearchHeuristic = 0;
                    _searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance) {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }
        return visibleCells;
    }


    private void ShowPath(int speed) {
        if (_currentPathExists) {
            HexCell current = _currentPathTo;
            while (current != _currentPathFrom) {
                int turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Colors.White);
                current = current.PathFrom;
            }
            _currentPathTo.EnableHighlight(Colors.Red);
            _currentPathFrom.EnableHighlight(Colors.Blue);
        } else if (_currentPathFrom is not null) {
            _currentPathFrom.DisableHighlight();
            _currentPathTo.DisableHighlight();
        }
    }

    private void CreateChunks() {
        _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];

        for (int z = 0, i = 0; z < _chunkCountZ; z++) {
            for (int x = 0; x < _chunkCountX; x++) {
                var chunk = this.InstantiateChild<HexGridChunk>(ChunkPrefab, $"Chunk_{x}-{z}");
                _chunks[i++] = chunk;
                chunk.RefreshStarted += () => _refreshStack++;
                chunk.RefreshCompleted += () => _refreshStack--;
            }
        }
    }

    private void CreateCells() {
        _cells = new HexCell[CellCountZ * CellCountX];
        for (int z = 0, i = 0; z < CellCountZ; z++) {
            for (int x = 0; x < CellCountX; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i) {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * HexMetrics.InnerRadius * 2.0f;
        position.Y = 0f;
        position.Z = z * HexMetrics.OuterRadius * 1.5f;

        HexCell cell = _cells[i] = this.InstantiateOrphan<HexCell>(HexCellPrefab, $"HexCell_{i}");
        cell.Position = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Index = i;
        cell.ShaderData = _cellShaderData;
        
        if (x > 0) {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }
        if (z > 0) {
            if ((z & 1) == 0) {
                cell.SetNeighbor(HexDirection.SE, _cells[i - CellCountX]);
                if (x > 0) {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - CellCountX - 1]);
                }
            }
            else {
                cell.SetNeighbor(HexDirection.SW, _cells[i - CellCountX]);
                if (x < CellCountX - 1) {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - CellCountX + 1]);
                }
            }
        }

        Label3D label = this.InstantiateOrphan<Label3D>(CellLabelPrefab);
        label.Position = new Vector3(position.X, label.Position.Y, position.Z);
        cell.Elevation = 0;
        cell.Label = label;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell) {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = _chunks[chunkX + chunkZ * _chunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);

    }

    private void ClearUnits() {
        for (int i = 0; i < _units.Count; i++) {
            _units[i].Die();
        }
        _units.Clear();
    }

    // private T InstantiateChild<T>(PackedScene scene, string name = null) where T : Node{
    //     T result = scene.Instantiate<T>();
    //     this.AddChild(result);
    //     if (name is not null) {
    //         result.Name = name;
    //     }
    //     return result;
    // }
    // 
    // private T InstantiateOrphan<T>(PackedScene scene, string name = null) where T : Node {
    //     T result = scene.Instantiate<T>();
    //     if (name is not null) {
    //         result.Name = name;
    //     }
    //     return result;
    // }
}
