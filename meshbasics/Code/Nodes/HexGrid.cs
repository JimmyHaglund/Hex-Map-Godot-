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

    private HexCell[] cells;
    public override void _Ready() {
        cells = new HexCell[Height * Width];

        for (int z = 0, i = 0; z < Height; z++) {
            for (int x = 0; x < Width; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateCell(int x, int z, int i) {
        Vector3 position;
        position.X = (x + z * 0.5f - z / 2) * HexMetrics.InnerRadius * 2.0f;
        position.Y = 0f;
        position.Z = z * HexMetrics.OuterRadius * 1.5f;

        HexCell cell = cells[i] = InstantiateChild<HexCell>(HexCellPrefab, $"HexCell_{i}");
        cell.Position = position;

        AddCellLabel(x, z, i, position);
    }

    private void AddCellLabel(int x, int z, int i, Vector3 worldPosition) {
        Label3D label = InstantiateChild<Label3D>(CellLabelPrefab);
        label.Position = new Vector3(worldPosition.X, label.Position.Y, worldPosition.Z);
        label.Text = $"{x}\n{z}";
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
