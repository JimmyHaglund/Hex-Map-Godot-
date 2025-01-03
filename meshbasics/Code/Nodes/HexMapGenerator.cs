using Godot;
using System;

namespace JHM.MeshBasics;

public sealed partial class HexMapGenerator : Node {
    [Export]public HexGrid Grid {get; set; }
    private Random _rng = new(1337);
    private int _cellCount;


    public void GenerateMap(int x, int z) {
        _cellCount = x * z;
        Grid.CreateMap(x, z);
        RaiseTerrain(7);
    }

    private void RaiseTerrain(int chunkSize) {
        for (int i = 0; i < chunkSize; i++) {
            GetRandomCell().TerrainTypeIndex = 1;
        }
    }

    HexCell GetRandomCell() {
        return Grid.GetCell((int)_rng.NextInt64(0, _cellCount));
    }

}
