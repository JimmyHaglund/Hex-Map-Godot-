using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGridChunk : Node3D {
    HexCell[] _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
    // Canvas GridCanvas;
    private bool _shouldUpdate = true;

    [Export] public HexMesh HexMesh { get; set; }

    public event Action RefreshStarted;
    public event Action RefreshCompleted;
    private bool _labelsVisible = false;

    // public override void _EnterTree() {
    //     // gridCanvas = GetComponentInChildren<Canvas>();
    //     
    //     _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
    // }

    public override void _Ready() {
        HexMesh.Triangulate(_cells);
    }

    public void AddCell(int index, HexCell cell) {
        _cells[index] = cell;
        cell.Chunk = this;
        this.AddChild(cell);

        if (cell.UiRect is not null) this.AddChild(cell.UiRect);
        cell.UiRect.Visible = _labelsVisible;
    }

    public void Refresh() {
        _shouldUpdate = true;// HexMesh.Triangulate(_cells);
    }

    public override void _Process(double delta) {
        if (_shouldUpdate) {
            CallDeferred("LateUpdate");
            RefreshStarted();
        }
    }

    private void LateUpdate() {
        HexMesh.Triangulate(_cells);
        _shouldUpdate = false;
        RefreshCompleted();
    }

    public void SetUIVisible(bool visible) {
        if (visible == _labelsVisible) return;
        _labelsVisible = visible;
        foreach (var cell in _cells) cell.SetShowLabel(visible);
    }
}
