using System.Linq;
using Godot;
using rts.Helpers;
using rts.scripts.hexTiles;
using rts.scripts.Player;
using rts.scripts.Units;

namespace rts.scripts.Environment;

/// <summary>
/// Anything a unit can gather a resource from: trees today, rocks or fishing spots later. The
/// resource type is an export rather than a subclass, so a new kind of node is a new scene with
/// different values, not new code.
/// </summary>
public partial class Harvestable : Node3DBase
{
    /// <summary>
    /// Scene tree group every harvestable joins on its own. Units search that group instead of
    /// walking the scene tree, and no scene has to carry a hand-maintained group entry.
    /// </summary>
    public const string GroupName = "harvestable";

    [Export] public PlayerResource.PlayerResourceType ResourceType { get; set; }

    [Export] public float ResourceAmount { get; set; } = 10f;

    /// <summary>
    /// The tile this node stands on. Resolved once: a harvestable never moves, and the search
    /// below would otherwise walk the parent chain of every node in the group on every scan.
    /// </summary>
    public HexTileBase Tile { get; private set; }

    // The unit currently working here. Without this, every idle unit would pick the same nearest
    // node and they would all pile up on one tree.
    private UnitBase _worker;

    public bool IsDepleted => ResourceAmount <= 0f;

    private bool IsReserved => _worker != null && IsInstanceValid(_worker);

    public override void _Ready()
    {
        AddToGroup(GroupName);
        Tile = HexTileBase.FindOwner(this);
    }

    public bool TryReserve(UnitBase unit)
    {
        if (unit == null
            || IsDepleted
            || (IsReserved && _worker != unit))
            return false;

        _worker = unit;

        return true;
    }

    /// <summary>
    /// Gives up this node's reservation. Only the unit holding it may release it, so a unit that
    /// lost its target cannot free someone else's.
    /// </summary>
    public void Release(UnitBase unit)
    {
        if (_worker == unit)
            _worker = null;
    }

    /// <summary>
    /// Takes up to <paramref name="requested"/> out of this node and returns what was actually
    /// available. The node removes itself once it runs out.
    /// </summary>
    public float Harvest(float requested)
    {
        if (requested <= 0f
            || IsDepleted)
            return 0f;

        var harvested = Mathf.Min(requested, ResourceAmount);
        ResourceAmount -= harvested;

        if (IsDepleted)
            QueueFree();

        return harvested;
    }

    /// <summary>
    /// Finds the closest unclaimed node of the given type standing on a tile at most
    /// <paramref name="tileRadius"/> hex steps from <paramref name="centerTile"/>. Reach is
    /// counted in whole tiles so it matches the highlighted tiles exactly, while
    /// <paramref name="center"/> only picks the nearest of the candidates - measured on the XZ
    /// plane only, because a node's origin sits at its base while the searching unit stands on
    /// the tile surface.
    /// </summary>
    public static Harvestable FindNearestFree(
        SceneTree tree,
        Vector3 center,
        Vector2I centerTile,
        int tileRadius,
        PlayerResource.PlayerResourceType type)
        => tree?.GetNodesInGroup(GroupName)
            .OfType<Harvestable>()
            .Where(harvestable => harvestable.ResourceType == type
                                  && !harvestable.IsDepleted
                                  && !harvestable.IsReserved
                                  && harvestable.Tile != null
                                  && WorldHelper.AxialDistance(centerTile, harvestable.Tile.AxialCoordinates)
                                  <= tileRadius)
            .MinBy(harvestable => FlatDistanceTo(center, harvestable.GlobalPosition));

    private static float FlatDistanceTo(Vector3 from, Vector3 to)
        => new Vector2(to.X - from.X, to.Z - from.Z).Length();
}