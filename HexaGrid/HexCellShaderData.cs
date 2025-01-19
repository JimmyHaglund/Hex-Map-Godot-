using Godot;
namespace JHM.HexaGrid;

public sealed class HexCellShaderData {
    private const float _transitionSpeed = 1.0f;
    private ImageTexture? _cellTexture;
    private Image? _image;
    private Color[]? _cellTextureData;
    private List<HexCell> _transitioningCells = new();
    private bool _needsVisibilityReset;

    public bool ImmediateMode { get; set; } = false;
    public HexGrid? Grid { get; set; }
    public bool HasTransitioningCells => _transitioningCells.Count > 0;

    public event Action<bool>? ActiveSet;

    public void SetMapData(HexCell cell, float data) {
        _cellTextureData[cell.Index].B = data < 0.0f ? 0.0f :
            (data < 1.0f ? (254.0f / 255.0f) * data : 254.0f / 255.0f);
        ActiveSet?.Invoke(true);
    }

    public void Initialize(int x, int z) {
        if (_cellTexture is not null) {
            _cellTexture.Dispose();
            _image!.Dispose();
            _cellTexture = null;
            _image = null;
        }
        _image = Image.CreateEmpty(x, z, useMipmaps: false, Image.Format.Rgba8);
        _cellTexture = ImageTexture.CreateFromImage(_image);
        Vector2 texelSize = new(1.0f / _cellTexture.GetWidth(), 1.0f / _cellTexture.GetHeight());
        RenderingServer.GlobalShaderParameterSet("HEX_TEXEL_SIZE", texelSize);
        RenderingServer.GlobalShaderParameterSet("HEX_CELL_DATA", _cellTexture);


        if (_cellTextureData == null || _cellTextureData.Length != x * z) {
            _cellTextureData = new Color[x * z];
        }
        else {
            for (int i = 0; i < _cellTextureData.Length; i++) {
                _cellTextureData[i] = new Color(0, 0, 0, 0);
            }
        }
        _transitioningCells.Clear();
        ActiveSet?.Invoke(true);
    }

    public void RefreshTerrain(HexCell cell) {
        _cellTextureData![cell.Index].A = (float)cell.TerrainTypeIndex / 4.0f;
        ActiveSet?.Invoke(true);
    }

    public void RefreshVisibility(HexCell cell) {
        int index = cell.Index;
        if (ImmediateMode) {
            _cellTextureData[index].R = cell.IsVisible ? 1.0f : 0.0f;
            _cellTextureData[index].G = cell.IsExplored ? 1.0f : 0.0f;
        }
        else if (_cellTextureData[index].B < 1.0f) {
            _cellTextureData[index].B = 1.0f;
            _transitioningCells.Add(cell);
        }
        ActiveSet?.Invoke(true);
    }

    public void ViewElevationChanged() {
        _needsVisibilityReset = true;
        ActiveSet?.Invoke(true);
    }

    public void Update(float deltaTime) {
        if (_cellTexture is null || _image is null) return;
        if (_needsVisibilityReset) {
            _needsVisibilityReset = false;
            Grid.ResetVisibility();
        }

        var transitionDelta = deltaTime * _transitionSpeed;
        for (int i = 0; i < _transitioningCells.Count; i++) {
            var isFinished = UpdateCellData(_transitioningCells[i], transitionDelta);
            if (isFinished) {
                _transitioningCells[i--] = _transitioningCells[^1];
                _transitioningCells.RemoveAt(_transitioningCells.Count - 1);
            }
        }

        var w = _cellTexture.GetWidth();
        for (var x = 0; x < w; x++) {
            for (var y = 0; y < _cellTexture.GetHeight(); y++) {
                var c = _cellTextureData[x + w * y];
                _image.SetPixel(x, y, c);
            }
        }
        _cellTexture.Update(_image);

        if (!HasTransitioningCells) ActiveSet?.Invoke(false);
    }

    private bool UpdateCellData(HexCell cell, float delta) {
        int index = cell.Index;
        Color data = _cellTextureData[index];
        bool stillUpdating = false;

        if (cell.IsExplored && data.G < 1.0f) {
            stillUpdating = true;
            float t = data.G + delta;
            data.G = Mathf.Min(t, 1.0f);
        }

        if (cell.IsVisible) {
            if (data.R < 1.0f) {
                stillUpdating = true;
                float t = data.R + delta;
                data.R = Mathf.Min(t, 1.0f);
            }
        }
        else if (data.R > 0.0f) {
            stillUpdating = true;
            float t = data.R - delta;
            data.R = t < 0.0f ? 0.0f : t;
        }

        if (!stillUpdating) {
            data.B = 0.0f;
        }
        _cellTextureData[index] = data;
        return !stillUpdating;
    }
}
