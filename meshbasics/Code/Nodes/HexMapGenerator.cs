using Godot;
using System;

namespace JHM.MeshBasics;

public sealed partial class HexMapGenerator : Node {
    private Random _rng;
    private int _cellCount;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;
    [Export] private int _seed = 1337;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _jitterProbability = 0.25f;
    [Export(PropertyHint.Range, "20, 200")] private int _chunkSizeMin = 30;
    [Export(PropertyHint.Range, "20, 200")] private int _chunkSizeMax = 100;
    [Export(PropertyHint.Range, "5, 95")] private int _landPercentage = 50;
    [Export(PropertyHint.Range, "1, 5")] private int _waterLevel = 2;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _highRiseProbability = 0.25f;
    [Export(PropertyHint.Range, "0.0, 0.4")] private float _sinkProbability = 0.2f;
    [Export(PropertyHint.Range, "-4, 0")] private int _elevationMinimum = -2;
    [Export(PropertyHint.Range, "6, 10")] private int _elevationMaximum = 8;

    [Export]public HexGrid Grid {get; set; }



    public void GenerateMap(int x, int z) {
        _cellCount = x * z;
        Grid.CreateMap(x, z);
        if (_searchFrontier == null) {
            _searchFrontier = new HexCellPriorityQueue();
        }
        CreateLand();
        for (int i = 0; i < _cellCount; i++) {
            Grid.GetCell(i).WaterLevel = _waterLevel;
        }
        SetTerrainType();
        for (int i = 0; i < _cellCount; i++) {
            Grid.GetCell(i).SearchPhase = 0;
        }

        
    }

    public override void _EnterTree() {
        _rng = new(_seed);
    }

    private void CreateLand() {
        int landBudget = Mathf.RoundToInt(_cellCount * _landPercentage * 0.01f);
        while (landBudget > 0) {
            int chunkSize = (int)_rng.NextInt64(_chunkSizeMin, _chunkSizeMax - 1);
            if (_rng.NextDouble() < _sinkProbability) {
                landBudget = SinkTerrain(chunkSize, landBudget);
            }
            else {
                landBudget = RaiseTerrain(chunkSize + 1, landBudget);
            }
        }
    }

    private int RaiseTerrain(int chunkSize, int budget) {
        _searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int rise = _rng.NextDouble() < _highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = originalElevation + rise;
            if (newElevation > _elevationMaximum) {
                continue;
            }
            current.Elevation = newElevation;
            if (originalElevation < _waterLevel &&
                newElevation >= _waterLevel &&
                --budget == 0
            ) {
                break;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center); ;
                    neighbor.SearchHeuristic = _rng.NextDouble() < _jitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }
        _searchFrontier.Clear();
        return budget;
    }

    private int SinkTerrain(int chunkSize, int budget) {
        _searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int sink = _rng.NextDouble() < _highRiseProbability ? 2 : 1;
        int size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            int originalElevation = current.Elevation;
            int newElevation = current.Elevation - sink;
            if (newElevation < _elevationMinimum) {
                continue;
            }
            current.Elevation = newElevation;
            if (originalElevation >= _waterLevel &&
                newElevation < _waterLevel &&
                --budget == 0
            ) {
                budget += 1;
            }
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center); ;
                    neighbor.SearchHeuristic = _rng.NextDouble() < _jitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }
        _searchFrontier.Clear();
        return budget;
    }

    HexCell GetRandomCell() {
        return Grid.GetCell((int)_rng.NextInt64(0, _cellCount));
    }
    private void SetTerrainType() {
        for (int i = 0; i < _cellCount; i++) {
            HexCell cell = Grid.GetCell(i);
            if (!cell.IsUnderwater) {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }


}
