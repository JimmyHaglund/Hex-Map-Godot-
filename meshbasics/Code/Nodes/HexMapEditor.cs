using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexMapEditor : Node2D {
    [Export]
    public Color[] Colors { get; set; }
    [Export] public HexGrid HexGrid { get; set; }

    private Color activeColor;

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
        HexGrid.ColorCell(mousePosition, activeColor);
    }

    public void SelectColor(int index) {
        activeColor = Colors[index];
    }
}
