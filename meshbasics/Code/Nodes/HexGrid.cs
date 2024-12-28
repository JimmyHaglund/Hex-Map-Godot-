using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGrid : Node3D {
    private int _cellCountX;
    private int _cellCountY;
    // private HexMesh _hexMesh;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;

    [ExportCategory("HexGrid Dependencies")]
    [Export] public PackedScene HexCellPrefab { get; set; }
    [Export] public PackedScene CellLabelPrefab { get; set; }
    [Export] public Texture2D NoiseSource { get; set; }
    [Export] public PackedScene ChunkPrefab { get; set; }

    [ExportCategory("HexGrid Configuration")]
    [Export] public int ChunkCountX { get; set; } = 6;
    [Export] public int ChunkCountZ { get; set; } = 6;
    [Export] public Color DefaultColor { get; set; } = new(1, 1, 1);

    

    public override void _EnterTree() {
        HexMetrics.NoiseSource = NoiseSource.GetImage();

        // _hexMesh = this.GetChild<HexMesh>(0);
        _cellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        _cellCountY = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    private void CreateChunks() {
        _chunks = new HexGridChunk[ChunkCountX * ChunkCountZ];

        for (int z = 0, i = 0; z < ChunkCountZ; z++) {
            for (int x = 0; x < ChunkCountX; x++) {
                var chunk = InstantiateChild<HexGridChunk>(ChunkPrefab, $"Chunk_{x}-{z}");
                _chunks[i++] = chunk;
            }
        }
    }

    private void CreateCells() {
        _cells = new HexCell[_cellCountY * _cellCountX];
        for (int z = 0, i = 0; z < _cellCountY; z++) {
            for (int x = 0; x < _cellCountX; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    // public override void _Ready() {
    //     _hexMesh.Triangulate(_cells);
    // }

    public HexCell GetCell(Vector3 position) {
        var coordinates = HexCoordinates.FromPosition(position);
        int index = coordinates.X + coordinates.Z * _cellCountX + coordinates.Z / 2;
        if (index >= _cells.Length || index < 0) return null;
        return _cells[index];
    }

    // public void Refresh() {
    //     _hexMesh.Triangulate(_cells);
    // }

    private void CreateCell(int x, int z, int i) {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * HexMetrics.InnerRadius * 2.0f;
        position.Y = 0f;
        position.Z = z * HexMetrics.OuterRadius * 1.5f;

        HexCell cell = _cells[i] = InstantiateOrphan<HexCell>(HexCellPrefab, $"HexCell_{i}");
        cell.Position = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = DefaultColor;

        if (x > 0) {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }
        if (z > 0) {
            if ((z & 1) == 0) {
                cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX]);
                if (x > 0) {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX - 1]);
                }
            }
            else {
                cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX]);
                if (x < _cellCountX - 1) {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX + 1]);
                }
            }
        }


        Label3D label = InstantiateOrphan<Label3D>(CellLabelPrefab);
        label.Position = new Vector3(position.X, label.Position.Y, position.Z);
        label.Text = cell.Coordinates.ToStringOnSeparateLines();
        cell.Elevation = 0;
        cell.UiRect = label;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell) {
        int chunkX = x / HexMetrics.ChunkSizeX;
        int chunkZ = z / HexMetrics.ChunkSizeZ;
        HexGridChunk chunk = _chunks[chunkX + chunkZ * ChunkCountX];

        int localX = x - chunkX * HexMetrics.ChunkSizeX;
        int localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);

    }

    private T InstantiateChild<T>(PackedScene scene, string name = null) where T : Node{
        T result = scene.Instantiate<T>();
        this.AddChild(result);
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }

    private T InstantiateOrphan<T>(PackedScene scene, string name = null) where T : Node {
        T result = scene.Instantiate<T>();
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }
}
