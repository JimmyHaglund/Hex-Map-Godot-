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

    [Export] public HexMesh HexMesh { get; set; }

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
        this.AddChild(cell);
        if (cell.UiRect is not null) this.AddChild(cell.UiRect);
    }
}
