using System;
using System.Diagnostics.Metrics;
using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexGridChunk : Node3D {
    HexCell[] _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
    // Canvas GridCanvas;
    private bool _shouldUpdate = true;

    [Export] public HexMesh Terrain { get; set; }
    [Export] public HexMesh Rivers { get; set; }
    [Export] public HexMesh Roads { get; set; }

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
        for (int i = 0; i < _cells.Length; i++) {
            Triangulate(_cells[i]);
        }
        Terrain.Apply();
        Rivers.Apply();
        Roads.Apply();
    }

    private void Triangulate(HexCell cell) {
        for (HexDirection direction = HexDirection.NE; direction <= HexDirection.NW; direction++) {
            Triangulate(direction, cell);
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
        }

        if (direction <= HexDirection.SE) {
            TriangulateConnection(direction, cell, e);
        }
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
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        Terrain.AddTriangle(centerL, m.v1, m.v2);
        Terrain.AddTriangleColor(cell.Color);

        Terrain.AddQuad(centerL, center, m.v2, m.v3);
        Terrain.AddQuadColor(cell.Color);
        Terrain.AddQuad(center, centerR, m.v3, m.v4);
        Terrain.AddQuadColor(cell.Color);

        Terrain.AddTriangle(centerR, m.v4, m.v5);
        Terrain.AddTriangleColor(cell.Color);

        bool reversed = cell.IncomingRiver == direction;
        TriangulateRiverQuad(centerL, centerR, m.v2, m.v4, cell.RiverSurfaceY, 0.4f, reversed);
        TriangulateRiverQuad(m.v2, m.v4, e.v2, e.v4, cell.RiverSurfaceY, 0.6f, reversed);
    }

    private void TriangulateWithoutRiver(
        HexDirection direction,
        HexCell cell,
        Vector3 center,
        EdgeVertices e
    ) {
        TriangulateEdgeFan(center, e, cell.Color);

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
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

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

    private void TriangulateAdjacentToRiver(
        HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
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

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
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
            center += corner * 0.25f;
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

    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1) {
        HexCell neighbor = cell.GetNeighbor(direction);
        if (neighbor is null) return;
        Vector3 bridge = HexMetrics.GetBridge(direction);
        bridge.Y = neighbor.Position.Y - cell.Position.Y;
        EdgeVertices e2 = new(e1.v1 + bridge, e1.v5 + bridge);

        if (cell.HasRiverThroughEdge(direction)) {
            e2.v3.Y = neighbor.StreamBedY;
            TriangulateRiverQuad(
                e1.v2, e1.v4, e2.v2, e2.v4,
                cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                cell.HasIncomingRiver && cell.IncomingRiver == direction
            );
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope) {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor, cell.HasRoadThroughEdge(direction));
        }
        else {
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color, cell.HasRoadThroughEdge(direction));
        }

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

    private void TriangulateEdgeTerraces(
        EdgeVertices begin, HexCell beginCell,
        EdgeVertices end, HexCell endCell,
        bool hasRoad = false
    ) {
        EdgeVertices e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2, hasRoad);

        for (var i = 2; i < HexMetrics.TerraceSteps; i++) {
            EdgeVertices e1 = e2;
            Color c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2, hasRoad);
        }

        TriangulateEdgeStrip(e2, c2, end, c2, hasRoad);
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
            Terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void TriangulateCornerTerraces(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        Vector3 v3 = HexMetrics.TerraceLerp(begin, left, 1);
        Vector3 v4 = HexMetrics.TerraceLerp(begin, right, 1);
        Color c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        Color c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        Terrain.AddTriangle(begin, v3, v4);
        Terrain.AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v3;
            Vector3 v2 = v4;
            Color c1 = c3;
            Color c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            Terrain.AddQuad(v1, v2, v3, v4);
            Terrain.AddQuadColor(c1, c2, c3, c4);
        }

        Terrain.AddQuad(v3, v4, left, right);
        Terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void TriangulateCornerTerracesCliff(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        float b = 1.0f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0) b = -b;
        Vector3 boundary = HexMetrics.Perturb(begin).Lerp(HexMetrics.Perturb(right), b);
        Color boundaryColor = beginCell.Color.Lerp(rightCell.Color, b);

        TriangulateBoundaryTriangle(
            begin, beginCell, left, leftCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else {
            Terrain.AddTriangleUnPerturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
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
        Color boundaryColor = beginCell.Color.Lerp(leftCell.Color, b);

        TriangulateBoundaryTriangle(
            right, rightCell, begin, beginCell, boundary, boundaryColor
        );

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope) {
            TriangulateBoundaryTriangle(
                left, leftCell, right, rightCell, boundary, boundaryColor
            );
        }
        else {
            Terrain.AddTriangleUnPerturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }


    private void TriangulateBoundaryTriangle(
        Vector3 begin, HexCell beginCell,
        Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor
    ) {
        Vector3 v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        Color c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);


        Terrain.AddTriangleUnPerturbed(HexMetrics.Perturb(begin), v2, boundary);
        Terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++) {
            Vector3 v1 = v2;
            Color c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            Terrain.AddTriangleUnPerturbed(v1, v2, boundary);
            Terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        Terrain.AddTriangleUnPerturbed(v2, HexMetrics.Perturb(left), boundary);
        Terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color) {
        Terrain.AddTriangle(center, edge.v1, edge.v2);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.v2, edge.v3);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.v3, edge.v4);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.v4, edge.v5);
        Terrain.AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(
        EdgeVertices e1, Color c1,
        EdgeVertices e2, Color c2,
        bool hasRoad = false

    ) {
        Terrain.AddQuad(e1.v1, e1.v2, e2.v1, e2.v2);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.v2, e1.v3, e2.v2, e2.v3);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.v3, e1.v4, e2.v3, e2.v4);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.v4, e1.v5, e2.v4, e2.v5);
        Terrain.AddQuadColor(c1, c2);

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
