using Godot;
using rts.scripts.Buildings;
using rts.scripts.hexTiles;

namespace rts.scripts.Units;

public partial class UnitBase : Node3DBase
{
    [ExportGroup("Movement")]
    [Export] public float MovementSpeed { get; set; } = 0.5f;

    public BuildingBase HomeBuilding { get; set; }

    /// <summary>
    /// The point this unit belongs to and works around. Falls back to the unit's own position so
    /// a unit placed by hand in a scene, without a building, still behaves sanely.
    /// </summary>
    public Vector3 HomePosition => HomeBuilding != null && IsInstanceValid(HomeBuilding)
        ? HomeBuilding.GlobalPosition
        : GlobalPosition;

    /// <summary>
    /// The tile this unit works around, and the center of its home building's harvest radius.
    /// Falls back to the tile the unit itself stands on, for the same reason as
    /// <see cref="HomePosition"/>.
    /// </summary>
    public HexTileBase HomeTile => HexTileBase.FindOwner(
        HomeBuilding != null && IsInstanceValid(HomeBuilding) ? HomeBuilding : this);

    /// <summary>
    /// Walks toward a global target on the XZ plane and reports whether the unit has arrived.
    /// The unit keeps its own height: every tile's surface sits at the same level, and the unit
    /// was already placed on it when it spawned.
    /// Works in global space because units are children of their building, which itself sits on a
    /// randomly rotated tile - local coordinates would not survive the trip across tiles.
    /// </summary>
    protected bool MoveTowards(
        Vector3 globalTarget,
        double delta,
        float stopDistance)
    {
        var current = GlobalPosition;
        var toTarget = new Vector3(globalTarget.X - current.X, 0f, globalTarget.Z - current.Z);
        var distance = toTarget.Length();

        if (distance <= stopDistance)
            return true;

        // Clamped to the remaining distance so a long frame cannot overshoot the target.
        var step = Mathf.Min(MovementSpeed * (float)delta, distance - stopDistance);
        GlobalPosition = current + toTarget / distance * step;

        return false;
    }
}