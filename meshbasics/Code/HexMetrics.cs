using Godot;

namespace JHM.MeshBasics;

public static class HexMetrics {
    public const float OuterRadius = 10.0f;
    public const float InnerRadius = OuterRadius * 0.866025404f;

    public static Vector3[] Corners = {
        new (0.0f, 0.0f, OuterRadius),
        new (InnerRadius, 0.0f, 0.5f * OuterRadius),
        new (InnerRadius, 0.0f, -0.5f * OuterRadius),
        new (0.0f, 0.0f, -OuterRadius),
        new (-InnerRadius, 0.0f, -0.5f * OuterRadius),
        new (-InnerRadius, 0.0f, 0.5f * OuterRadius)
    };
}
