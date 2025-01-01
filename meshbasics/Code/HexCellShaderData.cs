using Godot;
namespace JHM.MeshBasics;

public sealed partial class HexCellShaderData : Node {
    private Image _cellTexture;
    private Color[] _cellTextureData;

    public void Initialize(int x, int z) {
        if (_cellTexture is not null) { 
            _cellTexture.Resize(x, z, Image.Interpolation.Nearest);
        } else {
            _cellTexture = Image.CreateEmpty(x, z, useMipmaps: false, Image.Format.Rgba8);
        }

        if (_cellTextureData == null || _cellTextureData.Length != x * z) {
            _cellTextureData = new Color[x * z];
        }
        else {
            for (int i = 0; i < _cellTextureData.Length; i++) {
                _cellTextureData[i] = new Color(0, 0, 0, 0);
            }
        }
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void RefreshTerrain(HexCell cell) {
        _cellTextureData[cell.Index].A = (byte)cell.TerrainTypeIndex;
    }

    public override void _Process(double delta) {
        CallDeferred("LateUpdate");
    }

    private void LateUpdate() {
        var w = _cellTexture.GetWidth();
        for (var x = 0; x < w; x++) { 
            for(var y = 0; y < _cellTexture.GetHeight(); y++) { 
                var c = _cellTextureData[x + w * y];
                _cellTexture.SetPixel(x, y, c);
            }
        }
        this.ProcessMode = ProcessModeEnum.Disabled;
    }



}
