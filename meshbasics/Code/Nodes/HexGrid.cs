using Godot;
using System;
using static System.Net.Mime.MediaTypeNames;

namespace JHM.MeshBasics;

public sealed partial class HexGrid : Node3D {
    [ExportCategory("HexGrid Dependencies")]
    [Export] public PackedScene HexCellPrefab { get; set; }
    [Export] public PackedScene CellLabelPrefab { get; set; }

    [ExportCategory("HexGrid Configuration")]
    [Export] public int Width { get; set; } = 6;
    [Export] public int Height { get; set; } = 6;
    [Export] public Color DefaultColor { get; set; } = new(1, 1, 1);
    [Export] public Color TouchedColor { get; set; } = new(230, 150, 210);

    private HexMesh _hexMesh;
    private HexCell[] _cells;

    public override void _EnterTree() {
        _cells = new HexCell[Height * Width];
        _hexMesh = this.GetChild<HexMesh>(0);

        GD.Print(_hexMesh.Name);
        for (int z = 0, i = 0; z < Height; z++) {
            for (int x = 0; x < Width; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    public override void _Ready() {
        _hexMesh.Triangulate(_cells);
    }

    public override void _PhysicsProcess(double delta) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            TryTouch();
        }
    }

    private void TryTouch() {
        var mousePosition = Mouse3D.MouseWorldPosition;
        var coordinates = HexCoordinates.FromPosition(mousePosition);
        int index = coordinates.X + coordinates.Z * Width + coordinates.Z / 2;
        if (index >= _cells.Length) return;

        HexCell cell = _cells[index];
        cell.Color = TouchedColor;
        _hexMesh.Triangulate(_cells);
        GD.Print(coordinates);
    }


    private void CreateCell(int x, int z, int i) {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * HexMetrics.InnerRadius * 2.0f;
        position.Y = 0f;
        position.Z = z * HexMetrics.OuterRadius * 1.5f;

        HexCell cell = _cells[i] = InstantiateChild<HexCell>(HexCellPrefab, $"HexCell_{i}");
        cell.Position = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
        cell.Color = DefaultColor;

        Label3D label = InstantiateChild<Label3D>(CellLabelPrefab);
        label.Position = new Vector3(position.X, label.Position.Y, position.Z);
        label.Text = cell.Coordinates.ToStringOnSeparateLines();
    }

    private T InstantiateChild<T>(PackedScene scene, string name = null) where T : Node{
        T result = scene.Instantiate<T>();
        this.AddChild(result);
        if (name is not null) {
            result.Name = name;
        }
        return result;
    }
}
