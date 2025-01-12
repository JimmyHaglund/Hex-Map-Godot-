using System;
using System.Diagnostics.Metrics;
using System.Reflection;
using Godot;
using static Godot.RenderingServer;
using JHM.HexaGrid;

namespace JHM.MeshBasics;

public sealed partial class HexGridChunkNode : Node3D {
    public HexGridChunk Chunk { get; private set; }

    private bool _shouldUpdate = true;
    [Export] public HexMeshNode Terrain { get; set; }
    [Export] public HexMeshNode Rivers { get; set; }
    [Export] public HexMeshNode Roads { get; set; }
    [Export] public HexMeshNode Water { get; set; }
    [Export] public HexMeshNode WaterShore { get; set; }
    [Export] public HexMeshNode Estuaries { get; set; }
    [Export] public HexFeatureManager Features { get; set; }

    public event Action RefreshStarted;
    public event Action RefreshCompleted;
    private bool _labelsVisible = true;

    public void AddCell(int index, HexCell cell) { 
        Chunk.AddCell(index, cell);
        if (cell.Label is not null) this.AddChild(cell.Label);
        cell.Label.Visible = _labelsVisible;
    }

    public void Refresh() { 
        ProcessMode = ProcessModeEnum.Inherit;
    }

    public void SetUIVisible(bool visible) => Chunk.SetUIVisible(visible);

    public void Triangulate() => Chunk.Triangulate();

    #region Lifetime

    public override void _Ready() {
        Chunk = new(
            Terrain.HexMesh,
            Rivers.HexMesh,
            Roads.HexMesh,
            Water.HexMesh,
            WaterShore.HexMesh,
            Estuaries.HexMesh,
            Features
        );
        Chunk.RefreshRequested += Refresh;
    }

    public override void _Process(double delta) {
        CallDeferred("LateUpdate");
        RefreshStarted();
    }

    private void LateUpdate() {
        Triangulate();
        ProcessMode = ProcessModeEnum.Disabled;
        RefreshCompleted();
    }
    #endregion
}
