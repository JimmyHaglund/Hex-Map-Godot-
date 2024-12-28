using Godot;
using static Godot.RenderingDevice;

namespace JHM.MeshBasics;

public static class HexMetrics {
    public const float OuterRadius = 10.0f;
    public const float SolidFactor = 0.75f;
    public const float ElevationStep = 5.0f;
    public const float Maxelevation = 5.0f;
    public const int TerracesPerSlope = 2;

    public const float InnerRadius = OuterRadius * 0.866025404f;
    public const float BlendFactor = 1.0f - SolidFactor;
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;
    public const float HorizontalTerraceStepSize = 1.0f / TerraceSteps;
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);

    public static Image NoiseSource { get; set; }

    public static Vector3[] Corners = {
        new (0.0f, 0.0f, OuterRadius),
        new (InnerRadius, 0.0f, 0.5f * OuterRadius),
        new (InnerRadius, 0.0f, -0.5f * OuterRadius),
        new (0.0f, 0.0f, -OuterRadius),
        new (-InnerRadius, 0.0f, -0.5f * OuterRadius),
        new (-InnerRadius, 0.0f, 0.5f * OuterRadius),
        new (0.0f, 0.0f, OuterRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection direction) {
        return Corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction) {
        return Corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction) {
        return Corners[(int)direction] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction) {
        return Corners[(int)direction + 1] * SolidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction) {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step) {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        a.X += (b.X - a.X) * h;
        a.Z += (b.Z - a.Z) * h;
        float v = ((step + 1) / 2) * HexMetrics.VerticalTerraceStepSize;
        a.Y += (b.Y - a.Y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step) {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        return a.Lerp(b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2) {
        int delta = elevation2 - elevation1;

        return delta switch {
            0 => HexEdgeType.Flat,
            1 => HexEdgeType.Slope,
            -1 => HexEdgeType.Slope,
            _ => HexEdgeType.Cliff

        };
    }

    public static Vector4 SampleNoise(Vector3 position) {
        var pixel = NoiseSource.GetPixel((int)position.X, (int)position.Z);
        return new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
    }
}
