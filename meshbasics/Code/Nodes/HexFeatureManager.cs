﻿using Godot;

namespace JHM.MeshBasics;

public sealed partial class HexFeatureManager : Node3D {
    [Export] public PackedSceneContainer[] UrbanPrefabs { get; set; }
    [Export] public PackedSceneContainer[] FarmPrefabs { get; set; }
    [Export] public PackedSceneContainer[] PlantPrefabs { get; set; }
    [Export] public HexMesh Walls { get; set; }

    private Node3D _container;

    private PackedScene PickPrefab(PackedSceneContainer[] collection, int level, float hash, float choice) {
        if (level > 0) {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++) {
                if (hash < thresholds[i]) {
                    return collection[i].Scenes[(int)(choice * collection[i].Scenes.Length)];
                }
            }
        }
        return null;
    }

    public void Clear() {
        if (_container != null) {
            _container.QueueFree();
        }
        _container = new Node3D();
        this.AddChild(_container);
        _container.Position = Position;
        _container.Name = "FeatureContainer";
        Walls.Clear();
    }

    public void Apply() {
        Walls.Apply();
    }

    public void AddFeature(HexCell cell, Vector3 position) {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        var prefab = PickPrefab(UrbanPrefabs, cell.UrbanLevel, hash.A, hash.D);
        var otherPrefab = PickPrefab(FarmPrefabs, cell.FarmLevel, hash.B, hash.D);
        var usedHash = hash.A;
        if (prefab != null && hash.B < hash.A) {
            prefab = otherPrefab;
            usedHash = hash.B;
        }
        else if (otherPrefab != null) {
            prefab = otherPrefab;
            usedHash = hash.B;
        }
        otherPrefab = PickPrefab(PlantPrefabs, cell.PlantLevel, hash.C, hash.D);
        if (prefab != null && hash.C < usedHash) {
            prefab = otherPrefab;
        }
        else if (otherPrefab != null) {
            prefab = otherPrefab;
        }
        if (prefab is null) return;

        var instance = _container.InstantiateChild<Node3D>(prefab);
        instance.Position = HexMetrics.Perturb(position);
        instance.Rotation = new(0.0f, 2 * Mathf.Pi * hash.E, 0.0f);
    }
    public void AddWall(
        EdgeVertices near,
        HexCell nearCell,
        EdgeVertices far,
        HexCell farCell,
        bool hasRiver,
        bool hasRoad
    ) {
        if (nearCell.Walled == farCell.Walled
            || nearCell.IsUnderwater
            || farCell.IsUnderwater
            || nearCell.GetEdgeType(farCell) == HexEdgeType.Cliff
        ) {
            return;
        }
        AddWallSegment(near.v1, far.v1, near.v2, far.v2);
        if (hasRiver || hasRoad) {
            AddWallCap(near.v2, far.v2);
            AddWallCap(far.v4, near.v4);
        }
        else {
            AddWallSegment(near.v2, far.v2, near.v3, far.v3);
            AddWallSegment(near.v3, far.v3, near.v4, far.v4);
        }
        AddWallSegment(near.v4, far.v4, near.v5, far.v5);
    }

    public void AddWall(
        Vector3 c1, HexCell cell1,
        Vector3 c2, HexCell cell2,
        Vector3 c3, HexCell cell3
    ) {
        if (cell1.Walled) {
            if (cell2.Walled) {
                if (!cell3.Walled) {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled) {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled) {
            if (cell3.Walled) {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled) {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    private void AddWallSegment(
        Vector3 nearLeft,
        Vector3 farLeft,
        Vector3 nearRight,
        Vector3 farRight
    ) {
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        Vector3 left = HexMetrics.WallLerp(nearLeft, farLeft);
        Vector3 right = HexMetrics.WallLerp(nearRight, farRight);

        Vector3 leftThicknessOffset =
            HexMetrics.GetWallThicknessOffset(nearLeft, farLeft);
        Vector3 rightThicknessOffset =
            HexMetrics.GetWallThicknessOffset(nearRight, farRight);

        float leftTop = left.Y + HexMetrics.WallHeight;
        float rightTop = right.Y + HexMetrics.WallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset; ;
        v2 = v4 = right - rightThicknessOffset;
        v3.Y = leftTop;
        v4.Y = rightTop; 
        Walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.Y = leftTop;
        v4.Y = rightTop;
        Walls.AddQuadUnperturbed(v2, v1, v4, v3);

        Walls.AddQuadUnperturbed(t1, t2, v3, v4);
    }

    private void AddWallSegment(
        Vector3 pivot, HexCell pivotCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        AddWallSegment(pivot, left, pivot, right);
    }

    private void AddWallCap(Vector3 near, Vector3 far) {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.GetWallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.Y = v4.Y = center.Y + HexMetrics.WallHeight;
        Walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

}
