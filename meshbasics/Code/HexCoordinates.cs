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

    public override string ToString() {
        return "(" +
            X.ToString() + ", " + Y.ToString() + ", " + Z.ToString() + ")";
    }

    public string ToStringOnSeparateLines() {
        return X.ToString() + "\n" + Y.ToString() + "\n" + Z.ToString();
    }
}
