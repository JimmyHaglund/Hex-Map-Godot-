using Godot;
using System;

namespace JHM.MeshBasics;

public sealed partial class HexMapGenerator : Node {
    private Random _rng = new(1337);
    private int _cellCount;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;
    [Export(PropertyHint.Range, "0.0, 1.0")] private float _jitterProbability = 0.25f;
    [Export(PropertyHint.Range, "20, 200")] private int _chunkSizeMin = 30;
    [Export(PropertyHint.Range, "20, 200")] private int _chunkSizeMax = 100;

    [Export]public HexGrid Grid {get; set; }



    public void GenerateMap(int x, int z) {
        _cellCount = x * z;
        Grid.CreateMap(x, z);
        if (_searchFrontier == null) {
            _searchFrontier = new HexCellPriorityQueue();
        }
        for (int i = 0; i < 5; i++) {
            RaiseTerrain((int)_rng.NextInt64(_chunkSizeMin, _chunkSizeMax));
        }
        for (int i = 0; i < _cellCount; i++) {
            Grid.GetCell(i).SearchPhase = 0;
        }
    }

    private void RaiseTerrain(int chunkSize) {
        _searchFrontierPhase += 1;
        HexCell firstCell = GetRandomCell();
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        HexCoordinates center = firstCell.Coordinates;

        int size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0) {
            HexCell current = _searchFrontier.Dequeue();
            current.TerrainTypeIndex = 1;
            size += 1;

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);
                if (neighbor != null && neighbor.SearchPhase < _searchFrontierPhase) {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center); ;
                    neighbor.SearchHeuristic = _rng.Next() < _jitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }
        _searchFrontier.Clear();
    }

    HexCell GetRandomCell() {
        return Grid.GetCell((int)_rng.NextInt64(0, _cellCount));
    }

}
