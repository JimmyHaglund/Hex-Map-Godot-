using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapEditor : Node2D {
    [Export]
    public Color[] Colors { get; set; }
    [Export] public HexGrid HexGrid { get; set; }

    private Color _activeColor;
    private int _activeElevation;

    public override void _Ready() {
        SelectColor(0);
    }

    public override void _PhysicsProcess(double _) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            HandleInput();
        }
    }

    void HandleInput() {
        var mousePosition = Mouse3D.MouseWorldPosition;
        EditCell(HexGrid.GetCell(mousePosition));
    }

    void EditCell(HexCell cell) {
        cell.Color = _activeColor;
        cell.Elevation = _activeElevation;
    }

    public void SetElevation(float elevationPercentage) {
        var result = HexMetrics.Maxelevation * elevationPercentage / 100;
        _activeElevation = (int)result;
    }

    public void SelectColor(int index) {
        _activeColor = Colors[index];
    }
}
