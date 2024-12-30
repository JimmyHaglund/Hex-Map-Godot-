using Godot;
using System.ComponentModel;
using System.Xml.Linq;

namespace JHM.MeshBasics;

public sealed partial class HexFeatureManager : Node3D {
    [Export] public PackedSceneContainer[] UrbanPrefabs { get; set; }
    [Export] public PackedSceneContainer[] FarmPrefabs { get; set; }
    [Export] public PackedSceneContainer[] PlantPrefabs { get; set; }
    [Export] public HexMesh Walls { get; set; }
    [Export] public PackedScene WallTower { get; set; }
    [Export] public PackedScene Bridge { get; set; }

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
        Vector3 farRight,
        bool addTower = false
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

        if (addTower) {
            var towerInstance = _container.InstantiateOrphan<Node3D>(WallTower);
            towerInstance.Position = (left + right) * 0.5f;
            Vector3 rightDirection = right - left;
            rightDirection.Y = 0.0f;
            _container.AddChild(towerInstance);
            towerInstance.LookAt(towerInstance.GlobalPosition + rightDirection.Rotated(Vector3.Up, Mathf.Pi / 2));
        }
    }

    private void AddWallSegment(
        Vector3 pivot, HexCell pivotCell,
        Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell
    ) {
        if (pivotCell.IsUnderwater) {
            return;
        }
        bool hasLeftWall = !leftCell.IsUnderwater &&
            pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        bool hasRighWall = !rightCell.IsUnderwater &&
            pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall) {
            if (hasRighWall) {
                HexHash hash = HexMetrics.SampleHashGrid(
                    (pivot + left + right) * (1f / 3f)
                );
                bool hasTower = false;
                if (leftCell.Elevation == rightCell.Elevation) { 
                    hasTower = hash.E < HexMetrics.WallTowerThreshold;
                }
                AddWallSegment(pivot, left, pivot, right, hasTower);

            } else if (leftCell.Elevation < rightCell.Elevation) {
                AddWallWedge(pivot, left, right);
            }
            else {
                AddWallCap(pivot, left);
            }
        }
        else if (hasRighWall) {
            if (rightCell.Elevation < leftCell.Elevation) {
                AddWallWedge(right, pivot, left);
            }
            else {
                AddWallCap(right, pivot);
            }
        }
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

    void AddWallWedge(Vector3 near, Vector3 far, Vector3 point) {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        Vector3 center = HexMetrics.WallLerp(near, far);
        Vector3 thickness = HexMetrics.GetWallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        Vector3 pointTop = point;
        point.Y = center.Y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.Y = v4.Y = pointTop.Y = center.Y + HexMetrics.WallHeight;

        Walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        Walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        Walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }

    public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2) {
        roadCenter1 = HexMetrics.Perturb(roadCenter1);
        roadCenter2 = HexMetrics.Perturb(roadCenter2);
        var instance = _container.InstantiateChild<Node3D>(Bridge);
        instance.LookAt(instance.Position + (roadCenter2 - roadCenter1));
        instance.Position = (roadCenter1 + roadCenter2) * 0.5f;
    }

}
