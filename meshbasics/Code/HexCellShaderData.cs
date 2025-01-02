using Godot;
namespace JHM.MeshBasics;

public sealed partial class HexCellShaderData : Node {
    [Export] private ShaderMaterial[] _shaders;

    private ImageTexture _cellTexture;
    private Image _image;
    private Color[] _cellTextureData;

    public void Initialize(int x, int z) {
        if (_cellTexture is not null) { 
            _image.Resize(x, z, Image.Interpolation.Nearest);
            _cellTexture.Update(_image);
            Vector2 texelSize = new(1.0f / _cellTexture.GetWidth(), 1.0f / _cellTexture.GetHeight());
            foreach (var material in _shaders) {
                material.SetShaderParameter("texel_size", texelSize);
            }
        } else {
            _image = Image.CreateEmpty(x, z, useMipmaps: false, Image.Format.Rgba8);
            _cellTexture = ImageTexture.CreateFromImage(_image);
            Vector2 texelSize = new(1.0f / _cellTexture.GetWidth(), 1.0f / _cellTexture.GetHeight());
            foreach (var material in _shaders) {
                material.SetShaderParameter("texel_size", texelSize);
                material.SetShaderParameter("hex_cell_data", _cellTexture);
            }
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
        _cellTextureData[cell.Index].A = (float)cell.TerrainTypeIndex / 4.0f;
        ProcessMode = ProcessModeEnum.Inherit;

    }

    public void RefreshVisibility(HexCell cell) {
        _cellTextureData[cell.Index].R = cell.IsVisible ? 1.0f : 0.0f;
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public override void _Process(double delta) {
        CallDeferred("LateUpdate");
    }

    private void LateUpdate() {
        var w = _cellTexture.GetWidth();
        for (var x = 0; x < w; x++) { 
            for(var y = 0; y < _cellTexture.GetHeight(); y++) { 
                var c = _cellTextureData[x + w * y];
                _image.SetPixel(x, y, c);
            }
        }
        _cellTexture.Update(_image);

        this.ProcessMode = ProcessModeEnum.Disabled;
    }
}
