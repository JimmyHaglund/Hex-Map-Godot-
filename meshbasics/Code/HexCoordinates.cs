using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JHM.MeshBasics;

public struct HexCoordinates {
    public int X { get; private set; }
    public int Z { get; private set; }
    public int Y => -X - Z;

    public HexCoordinates(int x, int z) {
        X = x;
        Z = z;
    }

    public static HexCoordinates FromOffsetCoordinates(int x, int z) {
        return new(x - z / 2, z);
    }

    public static HexCoordinates FromPosition(Vector3 position) {
        float x = position.X / (HexMetrics.InnerRadius * 2f);
        float y = -x;

        float offset = position.Z / (HexMetrics.OuterRadius * 3f);
        x -= offset;
        y -= offset;

        int iX = Mathf.RoundToInt(x);
        int iY = Mathf.RoundToInt(y);
        int iZ = Mathf.RoundToInt(-x - y);

        if (iX + iY + iZ != 0) {
            float dX = Mathf.Abs(x - iX);
            float dY = Mathf.Abs(y - iY);
            float dZ = Mathf.Abs(-x - y - iZ);

            if (dX > dY && dX > dZ) {
                iX = -iY - iZ;
            }
            else if (dZ > dY) {
                iZ = -iX - iY;
            }
        }

        return new HexCoordinates(iX, iZ);
    }

    public override string ToString() {
        return "(" +
            X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines() {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }

    public int DistanceTo(HexCoordinates other) {
        return (
            (X < other.X ? other.X - X : X - other.X) +
            (Y < other.Y ? other.Y - Y : Y - other.Y) +
            (Z < other.Z ? other.Z - Z : Z - other.Z)
        ) / 2;
    }
}
