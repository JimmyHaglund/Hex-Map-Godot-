using Godot;

namespace JHM.MeshBasics;

public static class HexMetrics {
    public const float OuterRadius = 10.0f;
    public const float InnerRadius = OuterRadius * 0.866025404f;
    public const float SolidFactor = 0.75f;
    public const float BlendFactor = 1.0f - SolidFactor;



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
}
