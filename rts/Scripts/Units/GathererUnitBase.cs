using Godot;
using rts.scripts.Environment;
using rts.scripts.Player;

namespace rts.scripts.Units;

/// <summary>
/// A unit that repeatedly walks out to a <see cref="Harvestable"/> near its home building, works
/// on it, and carries the yield back home. Which resource it goes after and how fast it works are
/// exports, so a lumberjack, a fisherman and a stonecutter are the same code with different scene
/// values.
/// </summary>
public partial class GathererUnitBase : UnitBase
{
    private enum State
    {
        Searching,
        MovingToTarget,
        Harvesting,
        ReturningHome
    }

    [ExportGroup("Harvesting")]
    [Export]
    public PlayerResource.PlayerResourceType HarvestType { get; set; }

    /// <summary>Seconds of work per swing.</summary>
    [Export] public float HarvestDuration { get; set; } = 7f;

    /// <summary>How much the unit carries per trip before it heads home.</summary>
    [Export] public float CarryCapacity { get; set; } = 10f;

    /// <summary>
    /// How often an idle unit looks for new work. Scanning every frame would put the whole search
    /// on the frame budget once many units sit idle with nothing left to harvest.
    /// </summary>
    [Export] public float SearchInterval { get; set; } = 1f;

    /// <summary>How close the unit gets before it counts as arrived.</summary>
    [Export] public float ReachDistance { get; set; } = 0.3f;

    private State _state = State.Searching;
    private Harvestable _target;
    private float _timer;
    private float _carried;

    private bool HasTarget => _target != null && IsInstanceValid(_target);

    public override void _Process(double delta)
    {
        switch (_state)
        {
            case State.Searching:
                TickSearching((float)delta);
                break;
            case State.MovingToTarget:
                TickMovingToTarget(delta);
                break;
            case State.Harvesting:
                TickHarvesting((float)delta);
                break;
            case State.ReturningHome:
                TickReturningHome(delta);
                break;
        }
    }

    // Releasing on the way out matters: a unit removed mid-job would otherwise leave its target
    // reserved forever, and no other unit could ever take it.
    public override void _ExitTree() => DropTarget();

    private void TickSearching(float delta)
    {
        _timer -= delta;

        if (_timer > 0f)
            return;

        _timer = SearchInterval;

        // Searched around the home building's tile, not around the unit, so a gatherer always
        // stays tied to the building it came from instead of wandering off tree by tree.
        var homeTile = HomeTile;

        if (homeTile == null)
            return;

        var target = Harvestable.FindNearestFree(
            GetTree(),
            HomePosition,
            homeTile.AxialCoordinates,
            HomeBuilding?.HarvestRadius ?? 0,
            HarvestType);

        if (target == null
            || !target.TryReserve(this))
            return;

        _target = target;
        _state = State.MovingToTarget;
    }

    private void TickMovingToTarget(double delta)
    {
        if (!HasTarget
            || _target.IsDepleted)
        {
            GiveUpTarget();
            return;
        }

        // A harvestable's origin sits at its base - the trunk of a tree - so plain ReachDistance
        // already puts the unit right next to the thing it is working on.
        if (!MoveTowards(_target.GlobalPosition, delta, ReachDistance))
            return;

        _timer = HarvestDuration;
        _state = State.Harvesting;
    }

    private void TickHarvesting(float delta)
    {
        if (!HasTarget
            || _target.IsDepleted)
        {
            GiveUpTarget();
            return;
        }

        _timer -= delta;

        if (_timer > 0f)
            return;

        _carried += _target.Harvest(CarryCapacity - _carried);

        if (_carried < CarryCapacity
            && HasTarget
            && !_target.IsDepleted)
        {
            _timer = HarvestDuration;
            return;
        }

        DropTarget();
        _state = State.ReturningHome;
    }

    private void TickReturningHome(double delta)
    {
        if (HomeBuilding == null
            || !IsInstanceValid(HomeBuilding))
        {
            Deliver();
            return;
        }

        // Unlike a harvestable, a building's origin sits in the middle of its body, so its own
        // footprint has to be added or the unit would walk into the wall. FootprintRadius rather
        // than GetLocalRadius(): the latter would measure this very unit along with the building.
        var stopDistance = HomeBuilding.FootprintRadius + ReachDistance;

        if (MoveTowards(HomePosition, delta, stopDistance))
            Deliver();
    }

    private void Deliver()
    {
        PlayerResources.Instance?[HarvestType]?.TryAdd(_carried);

        _carried = 0f;
        _timer = 0f;
        _state = State.Searching;
    }

    /// <summary>Target is gone or empty: head home if the trip was worth something, else look again.</summary>
    private void GiveUpTarget()
    {
        DropTarget();
        _state = _carried > 0f ? State.ReturningHome : State.Searching;
        _timer = 0f;
    }

    private void DropTarget()
    {
        if (HasTarget)
            _target.Release(this);

        _target = null;
    }
}