using Godot;

namespace JHM.MeshBasics;

public partial class TouchHex : Node {
    public override void _Process(double delta) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            TryTouch();
        }
    }

    private void TryTouch() {
        var mousePosition = Mouse3D.MouseWorldPosition;
        var coordinates = HexCoordinates.FromPosition(mousePosition);
        GD.Print(coordinates);
    }
}
