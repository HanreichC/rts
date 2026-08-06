using Godot;
using System;

namespace rts.Implementations;

/// <summary>Pure hex-grid math (flat-top, axial coords). No scene state.</summary>
public static class HexGrid
{
    private static readonly Random _rng = new();
    public static float RandomRotation() => _rng.Next(6) * 60f; // 0,60,120,180,240,300

    /// <summary>The six axial neighbor offsets.</summary>
    public static readonly Vector2I[] Directions =
    [
        new(1, 0), new(0, 1), new(-1, 1),
        new(-1, 0), new(0, -1), new(1, -1)
    ];

    public static Vector3 AxialToWorld(Vector2I hex, float hexSize)
    {
        float x = hexSize * Mathf.Sqrt(3.0f) * (hex.X + hex.Y / 2.0f);
        float z = hexSize * 1.5f * hex.Y;
        return new Vector3(x, 0f, z);
    }

    public static Vector2I WorldToAxial(Vector3 worldPos, float hexSize)
    {
        float qf = (Mathf.Sqrt(3f) / 3f * worldPos.X - 1f / 3f * worldPos.Z) / hexSize;
        float rf = (2f / 3f * worldPos.Z) / hexSize;
        return CubeRound(qf, rf);
    }

    private static Vector2I CubeRound(float qf, float rf)
    {
        float sf = -qf - rf;
        int q = Mathf.RoundToInt(qf);
        int r = Mathf.RoundToInt(rf);
        int s = Mathf.RoundToInt(sf);

        float qdiff = Mathf.Abs(q - qf);
        float rdiff = Mathf.Abs(r - rf);
        float sdiff = Mathf.Abs(s - sf);

        if (qdiff > rdiff && qdiff > sdiff) q = -r - s;
        else if (rdiff > sdiff) r = -q - s;

        return new Vector2I(q, r);
    }
}
