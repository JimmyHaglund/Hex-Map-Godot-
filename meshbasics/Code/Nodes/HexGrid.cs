using System.Diagnostics;
using System.IO;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGrid : Node3D {
    // private HexMesh _hexMesh;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;
    private int _chunkCountZ;
    private int _chunkCountX;

    [ExportCategory("HexGrid Dependencies")]
    [Export] public int CellCountX { get; set; } = 20;
    [Export] public int CellCountZ { get; set; } = 15;
    [Export] public PackedScene HexCellPrefab { get; set; }
    [Export] public PackedScene CellLabelPrefab { get; set; }
    [Export] public Texture2D NoiseSource { get; set; }
    [Export] public PackedScene ChunkPrefab { get; set; }

    [ExportCategory("HexGrid Configuration")]
    [Export] public int Seed { get; set; } = 1234;
    [Export] public Color[] Colors { get; set; }

    private int _refreshStack = 0;
    public bool IsRefreshing => _refreshStack > 0;

    public override void _EnterTree() {
        HexMetrics.NoiseSource = NoiseSource.GetImage();
        HexMetrics.InitializeHashGrid(Seed);
        HexMetrics.Colors = Colors;

        CreateMap(CellCountX, CellCountZ);
    }

    public HexCell GetCell(Vector3 position) {
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

    public void CreateMap(int cellCountX, int cellCountZ) {
        if (
            cellCountX <= 0 || cellCountX % HexMetrics.ChunkSizeX != 0 ||
            cellCountZ <= 0 || cellCountZ % HexMetrics.ChunkSizeZ != 0
        ) {
            GD.PrintErr("Unsupported map size.");
            return;
        }

        if (_chunks != null) {
            for (int i = 0; i < _chunks.Length; i++) {
                _chunks[i].QueueFree();
            }
        }

        CellCountX = cellCountX;
        CellCountZ = cellCountZ;

        _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();
        HexMapCamera.ValidatePosition();
    }

    public void Save(BinaryWriter writer) {
        writer.Write(CellCountX);
        writer.Write(CellCountZ);
        for (int i = 0; i < _cells.Length; i++) {
            _cells[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header) {
        int x = 20, z = 15;
        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }
        CreateMap(x, z);
        for (int i = 0; i < _cells.Length; i++) {
            _cells[i].Load(reader);
        }
        for (int i = 0; i < _chunks.Length; i++) {
            _chunks[i].Refresh();
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
        label.Text = cell.Coordinates.ToStringOnSeparateLines();
        cell.Elevation = 0;
        cell.UiRect = label;

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
