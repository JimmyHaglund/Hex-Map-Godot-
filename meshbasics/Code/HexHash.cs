using System;

namespace JHM.MeshBasics;

public struct HexHash {
    public float A { get; private set; }
    public float B { get; private set; }
    public float C { get; private set; }

    public static HexHash Create(Random rng) {
        return new() { 
            A = (float)rng.NextDouble(),
            B = (float)rng.NextDouble(),
            C = (float)rng.NextDouble()
        };
    }
}