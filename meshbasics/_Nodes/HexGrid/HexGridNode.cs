using System.IO;
using System.Collections.Generic;
using Godot;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexGridNode : Node3D {
    [ExportCategory("HexGrid Dependencies")]
    [Export] private PackedScene _hexUnitPrefab;
    [Export] public PackedScene CellLabelPrefab { get; set; }
    [Export] public Texture2D NoiseSource { get; set; }
    [Export] public PackedScene ChunkPrefab { get; set; }

    [ExportCategory("HexGridMetrics Configuration")]
    [Export] public int Seed { get; set; } = 1234;
    
    public HexGrid Grid { get; private set; }
    public int CellCountX  => Grid.CellCountX;
    public int CellCountZ => Grid.CellCountZ;
    public bool Wrapping { get => Grid?.Wrapping ?? false; set => Grid.Wrapping = value; }

    public bool HasPath => Grid.HasPath;

    public bool IsRefreshing => Grid.IsRefreshing;

    public override void _EnterTree() {
        HexMetrics.NoiseSource = NoiseSource.GetImage();
        HexMetrics.InitializeHashGrid(Seed);
        HexMetrics.wrapSize = Wrapping ? CellCountX : 0;

        HexGridChunk.InstantiateChunkMethod = InstantiateChunk;

        Grid = new(this, ChunkPrefab) { 
            CellCountX = 15,
            CellCountZ = 20,
            CellLabelPrefab = CellLabelPrefab
        };
        Grid.CreateMap(CellCountX, CellCountZ, Wrapping);

        
        CreateMap(CellCountX, CellCountZ, Wrapping);
    }

    private static (Node3D, HexGridChunk) InstantiateChunk(PackedScene chunkPrefab) {
        var chunk = SceneInstantiator.InstantiateOrphan<HexGridChunkNode>(chunkPrefab);
        chunk.CreateChunk();

        return (chunk, chunk.Chunk);
    }

    public HexCell GetCell(Vector3 position) => Grid.GetCell(position);

    public HexCell GetCell(HexCoordinates coordinates) => Grid.GetCell(coordinates);

    public HexCell GetCell(int xOffset, int zOffset) => Grid.GetCell(xOffset, zOffset);

    public HexCell GetCell(int cellIndex) => Grid.GetCell(cellIndex);

    public void SetUIVisible(bool visible) => Grid.SetUIVisible(visible);

    public bool CreateMap(int cellCountX, int cellCountZ, bool wrap)  { 
        var mapWasCreated = Grid.CreateMap(cellCountX, cellCountZ, wrap);
        if (mapWasCreated) HexMapCamera.ValidatePosition();
        return mapWasCreated;
    }

    public void Save(BinaryWriter writer) => Grid.Save(writer);

    public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit) => Grid.FindPath(fromCell, toCell, unit);

    public void AddUnit(HexUnitNode unit, HexCell location, float orientation) => Grid.AddUnit(unit.Unit, location, orientation);

    public void MakeChildOfColumn(Node child, int columnIndex) => Grid.MakeChildOfColumn(child, columnIndex);

    public void RemoveUnit(HexUnit unit) => Grid.RemoveUnit(unit);

    public void ClearPath() => Grid.ClearPath();

    public new List<HexCell> GetPath() => Grid.GetPath();

    public void IncreaseVisibility(HexCell fromCell, int range) => Grid.IncreaseVisibility(fromCell, range);

    public void DecreaseVisibility(HexCell fromCell, int range) => Grid.DecreaseVisibility(fromCell, range);

    public void ResetVisibility() => Grid.ResetVisibility();

    public void CenterMap(float xPosition) => Grid.CenterMap(xPosition);
}
