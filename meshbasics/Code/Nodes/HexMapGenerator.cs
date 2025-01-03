using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapGenerator : Node {
    [Export]public HexGrid Grid {get; set; }

    public void GenerateMap(int x, int z) {
        Grid.CreateMap(x, z);
    }
}
