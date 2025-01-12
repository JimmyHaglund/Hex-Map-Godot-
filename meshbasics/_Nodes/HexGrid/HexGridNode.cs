using System.IO;
using System;
using Godot;
using System.Collections.Generic;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexGridNode : Node3D {
    public HexGrid  Grid { get; set; }
    // private HexMesh _hexMesh;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;
    private int _chunkCountZ;
    private int _chunkCountX;
    private Node3D[] _columns;

    [ExportCategory("HexGrid Dependencies")]
    [Export] private PackedScene _hexUnitPrefab;
    [Export] public int CellCountX { get; set; } = 20;
    [Export] public int CellCountZ { get; set; } = 15;

    [Export] public PackedScene CellLabelPrefab { get; set; }
    [Export] public Texture2D NoiseSource { get; set; }
    [Export] public PackedScene ChunkPrefab { get; set; }

    [ExportCategory("HexGrid Configuration")]
    [Export] public int Seed { get; set; } = 1234;
    
    public bool Wrapping { get => Grid.Wrapping; set => Grid.Wrapping = value; }

    public bool HasPath => Grid.HasPath;

    public bool IsRefreshing => Grid.IsRefreshing;

    public override void _EnterTree() {
        HexGridChunk.InstantiateChunkMethod = InstantiateChunk;
        HexMetrics.NoiseSource = NoiseSource.GetImage();
        HexMetrics.InitializeHashGrid(Seed);
        HexMetrics.wrapSize = Wrapping ? CellCountX : 0;
        CreateMap(CellCountX, CellCountZ, Wrapping);
    }

    private static (Node3D, HexGridChunk) InstantiateChunk(PackedScene chunkPrefab) {

        var chunk = SceneInstantiator.InstantiateOrphan<HexGridChunkNode>(chunkPrefab);
        return (chunk, chunk.Chunk);
    }

    public HexCell GetCell(Vector3 position) => Grid.GetCell(position);

    public HexCell GetCell(HexCoordinates coordinates) => Grid.GetCell(coordinates);

    public HexCell GetCell(int xOffset, int zOffset) => Grid.GetCell(xOffset, zOffset);

    public HexCell GetCell(int cellIndex) => Grid.GetCell(cellIndex);

    public void SetUIVisible(bool visible) {
        foreach (var chunk in _chunks) chunk.SetUIVisible(visible);
    }

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
