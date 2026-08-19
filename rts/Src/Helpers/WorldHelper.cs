using Godot;

namespace rts.Helpers;

public static class WorldHelper
{
    public static Vector3 AxialToWorld(Vector2I hex, float hexSize)
    {
        var x = hexSize * Mathf.Sqrt(3.0f) * (hex.X + hex.Y / 2.0f);
        var z = hexSize * 1.5f * hex.Y;
        return new Vector3(x, 0f, z);
    }

    public static Vector2I WorldToAxial(Vector3 worldPos, float hexSize)
    {
        var qf = (Mathf.Sqrt(3f) / 3f * worldPos.X - 1f / 3f * worldPos.Z) / hexSize;
        var rf = (2f / 3f * worldPos.Z) / hexSize;
        return CubeRound(qf, rf);
    }

    private static Vector2I CubeRound(float qf, float rf)
    {
        var sf = -qf - rf;
        var q = Mathf.RoundToInt(qf);
        var r = Mathf.RoundToInt(rf);
        var s = Mathf.RoundToInt(sf);

        var qdiff = Mathf.Abs(q - qf);
        var rdiff = Mathf.Abs(r - rf);
        var sdiff = Mathf.Abs(s - sf);

        if (qdiff > rdiff && qdiff > sdiff) q = -r - s;
        else if (rdiff > sdiff) r = -q - s;

        return new Vector2I(q, r);
    }
}