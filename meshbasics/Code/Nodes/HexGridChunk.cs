using System;
using System.Diagnostics.Metrics;
using Godot;
using static Godot.RenderingServer;

namespace JHM.MeshBasics;

public sealed partial class HexGridChunk : Node3D {
    HexCell[] _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
    // Canvas GridCanvas;
    private bool _shouldUpdate = true;
    private static Color _splatColor1 = new Color(1.0f, 0.0f, 0.0f);
    private static Color _splatColor2 = new Color(0.0f, 1.0f, 0.0f);
    private static Color _splatColor3 = new Color(0.0f, 0.0f, 1.0f);

    [Export] public HexMesh Terrain { get; set; }
    [Export] public HexMesh Rivers { get; set; }
    [Export] public HexMesh Roads { get; set; }
    [Export] public HexMesh Water { get; set; }
    [Export] public HexMesh WaterShore { get; set; }
    [Export] public HexMesh Estuaries { get; set; }
    [Export] public HexFeatureManager Features { get; set; }

    public event Action RefreshStarted;
    public event Action RefreshCompleted;
    private bool _labelsVisible = false;

    public override void _Ready() {
        Triangulate();
    }

    public override void _Process(double delta) {
        if (_shouldUpdate) {
            CallDeferred("LateUpdate");
            RefreshStarted();
        }
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

    private void LateUpdate() {
        Triangulate();
        _shouldUpdate = false;
        RefreshCompleted();
    }

    public void SetUIVisible(bool visible) {
        if (visible == _labelsVisible) return;
        _labelsVisible = visible;
        foreach (var cell in _cells) cell.SetShowLabel(visible);
    }

    public void Triangulate() {
        Terrain.Clear();
        Rivers.Clear();
        Roads.Clear();
        Water.Clear();
        WaterShore.Clear();
        Estuaries.Clear();
        Features.Clear();
        for (int i = 0; i < _cells.Length; i++) {
            Triangulate(_cells[i]);
        }
        Terrain.Apply();
        Rivers.Apply();
        Roads.Apply();
        Water.Apply();
        WaterShore.Apply();
        Estuaries.Apply();
        Features.Apply();
    }

    private void Triangulate(HexCell cell) {
        if (cell is null || cell.IsQueuedForDeletion()) return;
        for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++) {
            Triangulate(direction, cell);
        }
        if (!cell.IsUnderwater) {
            if (!cell.HasRiver && !cell.HasRoads) {
                Features.AddFeature(cell, cell.Position);
            }
            if (cell.IsSpecial) {
                Features.AddSpecialFeature(cell, cell.Position);
            }
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell) {
        Vector3 center = cell.Position;
        EdgeVertices e = new(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiver) {
            if (cell.HasRiverThroughEdge(direction)) {
                e.v3.Y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd) {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else {
            TriangulateWithoutRiver(direction, cell, center, e);
            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction)) {
                Features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, e);
        }

        if (cell.IsUnderwater) {
            TriangulateWater(direction, cell, center);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1) {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor is null) return;
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.Y = neighbor.Position.Y - cell.Position.Y;
        EdgeVertices e2 = new(e1.v1 + bridge, e1.v5 + bridge);

        bool hasRiver = cell.HasRiverThroughEdge(direction);
        bool hasRoad = cell.HasRoadThroughEdge(direction);

        if (hasRiver) {
            e2.v3.Y = neighbor.StreamBedY;
            if (!cell.IsUnderwater) {
                if (!neighbor.IsUnderwater) {

                    TriangulateRiverQuad(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                        cell.HasIncomingRiver && cell.IncomingRiver == direction
                    );
                }
                else if (cell.Elevation > neighbor.WaterLevel) {
                    TriangulateWaterfallInWater(
                        e1.v2, e1.v4, e2.v2, e2.v4,
                        cell.RiverSurfaceY, neighbor.RiverSurfaceY,
                        neighbor.WaterSurfaceY
                    );
                }
            } else if (
                !neighbor.IsUnderwater &&
                neighbor.Elevation > cell.WaterLevel
            ) {
                TriangulateWaterfallInWater(
                    e2.v4, e2.v2, e1.v4, e1.v2,
                    neighbor.RiverSurfaceY, cell.RiverSurfaceY,
                    cell.WaterSurfaceY
                );
            }
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, hasRoad);
        }
        else {
            TriangulateEdgeStrip(e1, _splatColor1, cell.TerrainTypeIndex,  e2, _splatColor2, cell.TerrainTypeIndex, hasRoad);
        }

        Features.AddWall(e1, cell, e2, neighbor, hasRiver, hasRoad);

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor is not null) {
            Vector3 v5 = e1.v5 + HexMetrics.GetBridge(direction.Next());
            v5.Y = nextNeighbor.Position.Y;

            if (cell.Elevation <= neighbor.Elevation) {
                if (cell.Elevation <= nextNeighbor.Elevation) {
                    // If the cell is the lowest (or tied for lowest) of its neighbors, use it as the bottom one.
                    TriangulateCorner(e1.v5, cell, e2.v5, neighbor, v5, nextNeighbor);
                }
                else {
                    // If nextNeighbor is lowest...
                    TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation) {
                TriangulateCorner(e2.v5, neighbor, v5, nextNeighbor, e1.v5, cell);
            }
            else {
                TriangulateCorner(v5, nextNeighbor, e1.v5, cell, e2.v5, neighbor);
            }

            // AddTriangle(v2, v4, v5);
            // AddTriangleColor(cell.Color, neighbor.Color, nextNeighbor.Color);
        }
    }

    private void TriangulateCorner(
        Vector3 bottom, HexCell bottomCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        HexEdgeType leftEdgeType = bottomCell.GetEdgeType(leftCell);
        HexEdgeType rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope) {
            if (rightEdgeType == HexEdgeType.Slope) {
                TriangulateCornerTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
            else if (rightEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
            else {
                TriangulateCornerTerracesCliff(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope) {
            if (leftEdgeType == HexEdgeType.Flat) {
                TriangulateCornerTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else {
                TriangulateCornerCliffTerraces(
                    bottom, bottomCell, left, leftCell, right, rightCell
                );
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            if (leftCell.Elevation < rightCell.Elevation) {
                TriangulateCornerCliffTerraces(
                    right, rightCell, bottom, bottomCell, left, leftCell
                );
            }
            else {
                TriangulateCornerTerracesCliff(
                    left, leftCell, right, rightCell, bottom, bottomCell
                );
            }
        }
        else {
            Terrain.AddTriangle(bottom, left, right);
            Terrain.AddTriangleColor(_splatColor1, _splatColor2, _splatColor3);
        }

        Features.AddWall(bottom, bottomCell, left, leftCell, right, rightCell);
    }

    private void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(_splatColor1, _splatColor2, 1);
        Color c4 = HexMetrics.TerraceLerp(_splatColor1, _splatColor3, 1);

        Terrain.AddTriangle(begin, v3, v4);
        Terrain.AddTriangleColor(_splatColor1, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(_splatColor1, _splatColor2, i);
            c4 = HexMetrics.TerraceLerp(_splatColor1, _splatColor3, i);
            Terrain.AddQuad(v1, v2, v3, v4);
            Terrain.AddQuadColor(c1, c2, c3, c4);
        }

        Terrain.AddQuad(v3, v4, left, right);
        Terrain.AddQuadColor(c3, c4, _splatColor2, _splatColor3);
    }

    private void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        float b = 1.0f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(right), b);
        Color boundaryColor = _splatColor1.Lerp(_splatColor3, b);

        TriangulateBoundaryTriangle(
            begin, _splatColor1, left, _splatColor2, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(
                left, _splatColor2, right, _splatColor3, boundary, boundaryColor
            );
        }
        else {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(_splatColor2, _splatColor3, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        float b = 1.0f / (leftCell.Elevation - beginCell.Elevation);
        b *= Mathf.Sign(b);
        Vector3 boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(left), b);
        Color boundaryColor = _splatColor1.Lerp(_splatColor2, b);

        TriangulateBoundaryTriangle(
            right, _splatColor3, begin, _splatColor1, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(
                left, _splatColor2, right, _splatColor3, boundary, boundaryColor
            );
        }
        else {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(_splatColor2, _splatColor3, boundaryColor);
        }
    }

    private void TriangulateWater(
        HexDirection direction, HexCell cell, Vector3 center
    ) {
        center.Y = cell.WaterSurfaceY;

        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor != null && !neighbor.IsUnderwater) {
            TriangulateWaterShore(direction, cell, neighbor, center);
        }
        else {
            TriangulateOpenWater(direction, cell, neighbor, center);
        }
    }

    private void TriangulateOpenWater(
        HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center
    ) {
        Vector3 c1 = center + HexMetrics.GetFirstWaterCorner(direction);
        Vector3 c2 = center + HexMetrics.GetSecondWaterCorner(direction);

        Water.AddTriangle(center, c1, c2);

        if (direction <= HexDirection.SE && neighbor != null) {
            Vector3 bridge = HexMetrics.GetWaterBridge(direction);
            Vector3 e1 = c1 + bridge;
            Vector3 e2 = c2 + bridge;

            Water.AddQuad(c1, c2, e1, e2);

            if (direction <= HexDirection.E) {
                HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
                if (nextNeighbor == null || !nextNeighbor.IsUnderwater) {
                    return;
                }
                Water.AddTriangle(
                    c2, e2, c2 + HexMetrics.GetWaterBridge(direction.Next())
                );
            }
        }
    }

    private void TriangulateWaterShore(
        HexDirection direction,
        HexCell cell,
        HexCell neighbor,
        Vector3 center
    ) {
        EdgeVertices e1 = new EdgeVertices(
            center + HexMetrics.GetFirstWaterCorner(direction),
            center + HexMetrics.GetSecondWaterCorner(direction)
        );
        Water.AddTriangle(center, e1.v1, e1.v2);
        Water.AddTriangle(center, e1.v2, e1.v3);
        Water.AddTriangle(center, e1.v3, e1.v4);
        Water.AddTriangle(center, e1.v4, e1.v5);

        Vector3 center2 = neighbor.Position;
        center2.Y = center.Y;

        EdgeVertices e2 = new EdgeVertices(
            center2 + HexMetrics.GetSecondSolidCorner(direction.Opposite()),
            center2 + HexMetrics.GetFirstSolidCorner(direction.Opposite())
        );
        if (cell.HasRiverThroughEdge(direction)) {
            TriangulateEstuary(e1, e2, cell.IncomingRiver == direction);
        }
        else {
            WaterShore.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
            WaterShore.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
            WaterShore.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
            WaterShore.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
            WaterShore.AddQuadUV(0f, 0f, 0f, 1f);
        }

        HexCell nextNeighbor = cell.GetNeighbor(direction.Next());
        if (nextNeighbor != null) {
            Vector3 v3 = nextNeighbor.Position + (nextNeighbor.IsUnderwater ?
                HexMetrics.GetFirstWaterCorner(direction.Previous()) :
                HexMetrics.GetFirstSolidCorner(direction.Previous()));
            v3.Y = center.Y;
            WaterShore.AddTriangle(
                e1.v5,
                e2.v5,
                v3
            );
            WaterShore.AddTriangleUV(
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, nextNeighbor.IsUnderwater ? 0f : 1f)
            );
        }
    }

    void TriangulateEstuary(EdgeVertices e1, EdgeVertices e2, bool incomingRiver) {
        WaterShore.AddTriangle(e2.v1, e1.v2, e1.v1);
        WaterShore.AddTriangle(e2.v5, e1.v5, e1.v4);
        WaterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        WaterShore.AddTriangleUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
        Estuaries.AddQuad(e2.v1, e1.v2, e2.v2, e1.v3);
        Estuaries.AddTriangle(e1.v3, e2.v2, e2.v4);
        Estuaries.AddQuad(e1.v3, e1.v4, e2.v4, e2.v5);

        Estuaries.AddQuadUV(
            new Vector2(0f, 1f), new Vector2(0f, 0f),
            new Vector2(1.0f, 1f), new Vector2(0f, 0f)
        );
        Estuaries.AddTriangleUV(
            new Vector2(0f, 0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f)
        );
        Estuaries.AddQuadUV(
            new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        );
        if (incomingRiver) {
            Estuaries.AddQuadUV2(
                new Vector2(1.5f, 1.0f), new Vector2(0.7f, 1.15f),
                new Vector2(1.0f, 0.8f), new Vector2(0.5f, 1.1f)
            );
            Estuaries.AddTriangleUV2(
                new Vector2(0.5f, 1.1f),
                new Vector2(1.0f, 0.8f),
                new Vector2(0.0f, 0.8f)
            );
            Estuaries.AddQuadUV2(
                new Vector2(0.5f, 1.1f), new Vector2(0.3f, 1.15f),
                new Vector2(0.0f, 0.8f), new Vector2(-0.5f, 1.0f)
            );
        } else {
            Estuaries.AddQuadUV2(
                    new Vector2(-0.5f, -0.2f), new Vector2(0.3f, -0.35f),
                    new Vector2(0f, 0f), new Vector2(0.5f, -0.3f)
                );
            Estuaries.AddTriangleUV2(
                new Vector2(0.5f, -0.3f),
                new Vector2(0f, 0f),
                new Vector2(1f, 0f)
            );
            Estuaries.AddQuadUV2(
                new Vector2(0.5f, -0.3f), new Vector2(0.7f, -0.35f),
                new Vector2(1f, 0f), new Vector2(1.5f, -0.2f)
            );
        }
    }

    void TriangulateWaterfallInWater(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float waterY
    ) {
        v1.Y = v2.Y = y1;
        v3.Y = v4.Y = y2;

        v1 = HexMetrics.Perturb(v1);
        v2 = HexMetrics.Perturb(v2);
        v3 = HexMetrics.Perturb(v3);
        v4 = HexMetrics.Perturb(v4);

        float t = (waterY - y2) / (y1 - y2);
        v3 = v3.Lerp(v1, t);
        v4 = v4.Lerp(v2, t);

        Rivers.AddQuadUnperturbed(v1, v2, v3, v4);
        Rivers.AddQuadUV(0f, 1f, 0.8f, 1f);
    }

    private void TriangulateWithRiver(
        HexDirection direction,
        HexCell cell,
        Vector3 center,
        EdgeVertices e
    ) {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite())) {
            centerL = center +
            HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center +
                HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next())) {
            centerL = center;
            centerR = center.Lerp(e.v5, 2.0f / 3.0f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous())) {
            centerL = center.Lerp(e.v1, 2.0f / 3.0f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2())) {
            centerL = center;
            centerR = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Next()) *
                (0.5f * HexMetrics.InnerToOuter);
        }
        else {
            centerL = center +
                HexMetrics.GetSolidEdgeMiddle(direction.Previous()) *
                (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }
        center = centerL.Lerp(centerR, 0.5f);

        EdgeVertices m = new EdgeVertices(
            centerL.Lerp(e.v1, 0.5f),
            centerR.Lerp(e.v5, 0.5f),
            1.0f / 6.0f
        );
        m.v3.Y = center.Y = e.v3.Y;
        TriangulateEdgeStrip(m, _splatColor1, cell.TerrainTypeIndex, e, _splatColor1, cell.TerrainTypeIndex);


        Terrain.AddTriangle(centerL, m.v1, m.v2);
        Terrain.AddQuad(centerL, center, m.v2, m.v3);
        Terrain.AddQuad(center, centerR, m.v3, m.v4);
        Terrain.AddTriangle(centerR, m.v4, m.v5);

        Terrain.AddTriangleColor(_splatColor1);
        Terrain.AddQuadColor(_splatColor1);
        Terrain.AddTriangleColor(_splatColor1);
        Terrain.AddQuadColor(_splatColor1);

        if (!cell.IsUnderwater) { 
            bool reversed = cell.IncomingRiver == direction;
            TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
            TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
        }
    }

    private void TriangulateWithoutRiver(
        HexDirection direction,
        HexCell cell,
        Vector3 center,
        EdgeVertices e
    ) {
        TriangulateEdgeFan(center, e, cell.TerrainTypeIndex);

        if (cell.HasRoads) {
            Vector2 interpolators = GetRoadInterpolators(direction, cell);
            TriangulateRoad(
                center,
                center.Lerp(e.v1, interpolators.X),
                center.Lerp(e.v5, interpolators.Y),
                e,
                cell.HasRoadThroughEdge(direction)
            );
        }
    }

    private void TriangulateWithRiverBeginOrEnd(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        EdgeVertices m = new EdgeVertices(
            center.Lerp(e.v1, 0.5f),
            center.Lerp(e.v5, 0.5f)
        );
        m.v3.Y = e.v3.Y;
        TriangulateEdgeStrip(m, _splatColor1, cell.TerrainTypeIndex, e, _splatColor1, cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

        if (!cell.IsUnderwater) { 
            bool reversed = cell.HasIncomingRiver;
            TriangulateRiverQuad(
                m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed
            );

            center.Y = m.v2.Y = m.v4.Y = cell.RiverSurfaceY;
            Rivers.AddTriangle(center, m.v2, m.v4);
            if (reversed) {
                Rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(1f, 0.2f),
                    new Vector2(0f, 0.2f)
                );
            }
            else {
                Rivers.AddTriangleUV(
                    new Vector2(0.5f, 0.4f),
                    new Vector2(0f, 0.6f),
                    new Vector2(1f, 0.6f)
                );
            }
        }
    }

    private void TriangulateAdjacentToRiver(
        HexDirection direction,
        HexCell cell,
        Vector3 center,
        EdgeVertices e
    ) {
        if (cell.HasRoads) {
            TriangulateRoadAdjacentToRiver(direction, cell, center, e);
        }

        if (cell.HasRiverThroughEdge(direction.Next())) {
            if (cell.HasRiverThroughEdge(direction.Previous())) {
                center += HexMetrics.GetSolidEdgeMiddle(direction) *
                    (HexMetrics.InnerToOuter * 0.5f);
            }
            else if (
                cell.HasRiverThroughEdge(direction.Previous2())
            ) {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (
            cell.HasRiverThroughEdge(direction.Previous()) &&
            cell.HasRiverThroughEdge(direction.Next2())
        ) {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        EdgeVertices m = new EdgeVertices(
            center.Lerp(e.v1, 0.5f),
            center.Lerp(e.v5, 0.5f)
        );

        TriangulateEdgeStrip(m, _splatColor1, cell.TerrainTypeIndex, e, _splatColor1, cell.TerrainTypeIndex);
        TriangulateEdgeFan(center, m, cell.TerrainTypeIndex);

        if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction)) {
            Features.AddFeature(cell, (center + e.v1 + e.v5) * (1f / 3f));
        }
    }

    private void TriangulateRoadAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
    ) {
        bool hasRoadThroughEdge = cell.HasRoadThroughEdge(direction);
        bool previousHasRiver = cell.HasRiverThroughEdge(direction.Previous());
        bool nextHasRiver = cell.HasRiverThroughEdge(direction.Next());
        Vector2 interpolators = GetRoadInterpolators(direction, cell);
        Vector3 roadCenter = center;

        if (cell.HasRiverBeginOrEnd) {
            roadCenter += HexMetrics.GetSolidEdgeMiddle(
                cell.RiverBeginOrEndDirection.Opposite()
            ) * (1.0f / 3.0f);
        } else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite()) {
            Vector3 corner;
            if (previousHasRiver) {
                if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Next())
                ) {
                    return;
                }
                corner = HexMetrics.GetSecondSolidCorner(direction);
            } else {
                if (
                    !hasRoadThroughEdge &&
                    !cell.HasRoadThroughEdge(direction.Previous())
                ) {
                    return;
                }
                corner = HexMetrics.GetFirstSolidCorner(direction);
            }
            roadCenter += corner * 0.5f;
            if (cell.IncomingRiver == direction.Next() && (
                cell.HasRoadThroughEdge(direction.Next2()) ||
                cell.HasRoadThroughEdge(direction.Opposite()))
            ) {
                Features.AddBridge(roadCenter, center - corner * 0.5f);
            }
            center += corner * 0.25f;
        } else if (cell.IncomingRiver == cell.OutgoingRiver.Previous()) {
            roadCenter -= HexMetrics.GetSecondCorner(cell.IncomingRiver) * 0.2f;
        } else if (cell.IncomingRiver == cell.OutgoingRiver.Next()) {
            roadCenter -= HexMetrics.GetFirstCorner(cell.IncomingRiver) * 0.2f;
        } else if (previousHasRiver && nextHasRiver) {
            if (!hasRoadThroughEdge) {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(direction) *
                HexMetrics.InnerToOuter;
            roadCenter += offset * 0.7f;
            center += offset * 0.5f;
        } else {
            HexDirection middle;
            if (previousHasRiver) {
                middle = direction.Next();
            }
            else if (nextHasRiver) {
                middle = direction.Previous();
            }
            else {
                middle = direction;
            }
            if (
                !cell.HasRoadThroughEdge(middle) &&
                !cell.HasRoadThroughEdge(middle.Previous()) &&
                !cell.HasRoadThroughEdge(middle.Next())
            ) {
                return;
            }
            Vector3 offset = HexMetrics.GetSolidEdgeMiddle(middle);
            roadCenter += offset * 0.25f;
            if (direction == middle &&
                cell.HasRoadThroughEdge(direction.Opposite())
            ) {
                    Features.AddBridge(
                    roadCenter,
                    center - offset * (HexMetrics.InnerToOuter * 0.7f)
                );
            }
        }

        Vector3 mL = roadCenter.Lerp(e.v1, interpolators.X);
        Vector3 mR = roadCenter.Lerp(e.v5, interpolators.Y);
        TriangulateRoad(roadCenter, mL, mR, e, hasRoadThroughEdge);

        if (previousHasRiver) {
            TriangulateRoadEdge(roadCenter, center, mL);
        }
        if (nextHasRiver) {
            TriangulateRoadEdge(roadCenter, mR, center);
        }
    }

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad = false
    ) {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(_splatColor1, _splatColor2, 1);

        TriangulateEdgeStrip(begin, _splatColor1, beginCell.TerrainTypeIndex, e2, c2, endCell.TerrainTypeIndex, hasRoad);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(_splatColor1, _splatColor2, i);
            TriangulateEdgeStrip(e1, c1, beginCell.TerrainTypeIndex, e2, c2, endCell.TerrainTypeIndex, hasRoad);
        }

        TriangulateEdgeStrip(e2, c2, beginCell.TerrainTypeIndex, end, _splatColor2, endCell.TerrainTypeIndex, hasRoad);
    }

    private void TriangulateBoundaryTriangle(
        Vector3 begin, Color beginColor,
        Vector3 left, Color leftColor,
        Vector3 boundary, Color boundaryColor
    ) {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginColor, leftColor, 1);


        Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        Terrain.AddTriangleColor(beginColor, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginColor, leftColor, i);
            Terrain.AddTriangleUnperturbed(v1, v2, boundary);
            Terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        Terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        Terrain.AddTriangleColor(c2, leftColor, boundaryColor);
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float type) {
        Terrain.AddTriangle(center, edge.v1, edge.v2);
        Terrain.AddTriangle(center, edge.v2, edge.v3);
        Terrain.AddTriangle(center, edge.v3, edge.v4);
        Terrain.AddTriangle(center, edge.v4, edge.v5);

        Terrain.AddTriangleColor(_splatColor1);
        Terrain.AddTriangleColor(_splatColor1);
        Terrain.AddTriangleColor(_splatColor1);
        Terrain.AddTriangleColor(_splatColor1);

        Vector3 types;
        types.X = types.Y = types.Z = type;
        Terrain.AddTriangleTerrainTypes(types);
        Terrain.AddTriangleTerrainTypes(types);
        Terrain.AddTriangleTerrainTypes(types);
        Terrain.AddTriangleTerrainTypes(types);
    }

    private void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1, float type1,
        EdgeVertices e2, Color c2, float type2,
        bool hasRoad = false

    ) {
        Terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        Terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        Terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        Terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);

        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuadColor(c1, c2);

        Vector3 types;
        types.X = types.Z = type1;
        types.Y = type2;
        Terrain.AddQuadTerrainTypes(types);
        Terrain.AddQuadTerrainTypes(types);
        Terrain.AddQuadTerrainTypes(types);
        Terrain.AddQuadTerrainTypes(types);

        if (hasRoad) {
            TriangulateRoadSegment(e1.v2, e1.v3, e1.v4, e2.v2, e2.v3, e2.v4);
        }
    }

    private void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y1, float y2, float v, bool reversed
    ) {
        v1.Y = v2.Y = y1;
        v3.Y = v4.Y = y2;
        Rivers.AddQuad(v1, v2, v3, v4);
        if (reversed) {
            Rivers.AddQuadUV(1.0f, 0.0f, 0.8f - v, 0.6f - v);
        } else { 
            Rivers.AddQuadUV(0.0f, 1.0f, v, v + 0.2f);
        }
    }

    private void TriangulateRiverQuad(
        Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
        float y, float v, bool reversed
    ) {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    private void TriangulateRoadSegment(
        Vector3 v1, Vector3 v2, Vector3 v3,
        Vector3 v4, Vector3 v5, Vector3 v6
    ) {
        Roads.AddQuad(v1, v2, v4, v5);
        Roads.AddQuad(v2, v3, v5, v6);
        Roads.AddQuadUV(0f, 1f, 0f, 0f);
        Roads.AddQuadUV(1f, 0f, 0f, 0f);
    }

    private void TriangulateRoad(
        Vector3 center,
        Vector3 mL,
        Vector3 mR,
        EdgeVertices e,
        bool hasRoadThroughCellEdge
    ) {
        if (hasRoadThroughCellEdge) {
            Vector3 mC = mL.Lerp(mR, 0.5f);
            TriangulateRoadSegment(mL, mC, mR, e.v2, e.v3, e.v4);
            Roads.AddTriangle(center, mL, mC);
            Roads.AddTriangle(center, mC, mR);

            Roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(1f, 0f)
            );
            Roads.AddTriangleUV(
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f)
            );
        } else {
            TriangulateRoadEdge(center, mL, mR);
        }
    }

    private void TriangulateRoadEdge(Vector3 center, Vector3 mL, Vector3 mR) {
        Roads.AddTriangle(center, mL, mR);
        Roads.AddTriangleUV(
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f)
        );
    }

    private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell) {
        Vector2 interpolators;
        if (cell.HasRoadThroughEdge(direction)) {
            interpolators.X = interpolators.Y = 0.5f;
        } else {
            interpolators.X =
                cell.HasRoadThroughEdge(direction.Previous()) ? 0.5f : 0.25f;
            interpolators.Y =
                cell.HasRoadThroughEdge(direction.Next()) ? 0.5f : 0.25f;
        }
        return interpolators;
    }
}
