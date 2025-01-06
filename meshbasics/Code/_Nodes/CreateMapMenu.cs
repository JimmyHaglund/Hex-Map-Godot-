using Godot;

namespace JHM.MeshBasics;

public sealed partial class CreateMapMenu : Control {
    [Export] private HexGrid _grid;
    [Export] private HexMapGenerator _generator;

    private bool _generate = true;
    private bool _wrap = true;

    public void SetGenerate(bool value) => _generate = value;

    public void CreateMap(int x, int z) { 
        if (_generate) { 
            _generator.GenerateMap(x, z, _wrap);
        } else { 
            _grid.CreateMap(x, z, _wrap);
        }
    }

    public void SetWrapping(bool value) => _wrap = value;
}
