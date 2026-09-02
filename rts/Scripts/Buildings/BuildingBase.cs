using Godot;
using rts.Exceptions;
using rts.scripts.Player;
using rts.scripts.Units;

namespace rts.scripts.Buildings;

public partial class BuildingBase : Node3DBase
{
    [ExportGroup("Building Costs")]
    [Export]
    public PlayerResource.PlayerResourceType CostType { get; set; }

    [Export] public float CostValue { get; set; }

    [ExportGroup("Resource Generation")]
    [Export]
    public PlayerResource.PlayerResourceType GenerationType { get; set; }

    [Export] public float GenerationValue { get; set; }

    [Export] public float GenerationInterval { get; set; }

    [ExportGroup("Units")]
    [Export]
    public PackedScene UnitScene { get; set; }

    [Export]
    public int UnitCount { get; set; }

    [Export]
    public float UnitSpawnRadius { get; set; }

    private PlayerResource CostResource => PlayerResources.Instance[CostType];

    private static PlayerResource BuildingResource =>
        PlayerResources.Instance[PlayerResource.PlayerResourceType.Building];

    private PlayerResource GenerationResource => PlayerResources.Instance[GenerationType];

    private float? _footprintRadius;

    /// <summary>
    /// How far the building's own body reaches out from its origin, measured once before it
    /// spawns anything. Units are children of their building, so measuring on demand would
    /// include whichever unit currently stands furthest away and the value would grow as they
    /// walk off - a unit heading home would then already count as arrived where it stands.
    /// </summary>
    public float FootprintRadius => _footprintRadius ??= GetLocalRadius();

    public void EnsureBuildRequirementsMet()
    {
        if (CostResource is null
            || !CostResource.CanSubtract(CostValue)
            || BuildingResource is null
            || !BuildingResource.CanIncrement())
            throw new BuildRequirementsAreNotMetException();
    }

    public void SpendBuildRequirements()
    {
        CostResource?.TrySubtract(CostValue);
        BuildingResource?.TryIncrement();
    }

    public override void _Ready()
    {
        SpawnUnits();

        if (GenerationResource is null
            || GenerationValue <= 0
            || GenerationInterval <= 0)
            return;

        var timer = new Timer
        {
            WaitTime = GenerationInterval,
            Autostart = true
        };

        AddChild(timer);
        timer.Timeout += OnGenerationTimerTimeout;
    }

    private void OnGenerationTimerTimeout()
        => GenerationResource.TryAdd(GenerationValue);
    
    /// <summary>
    /// Places the building's units in a ring beside it. The ring starts at the building's own
    /// footprint and grows by each unit's footprint, so <see cref="UnitSpawnRadius"/> is the
    /// plain gap between building and unit instead of a per-model hardcoded distance.
    /// </summary>
    private void SpawnUnits()
    {
        if (UnitScene == null
            || UnitCount <= 0)
            return;

        var groundY = GetLocalBottomY();
        // Touched before the first unit is added, so the cached value is the bare building.
        var buildingRadius = FootprintRadius;

        for (var i = 0; i < UnitCount; i++)
        {
            if (UnitScene.Instantiate() is not UnitBase unit)
                continue;

            AddChild(unit);
            unit.HomeBuilding = this;

            var angle = Mathf.Tau * i / UnitCount;
            var distance = buildingRadius + UnitSpawnRadius + unit.GetLocalRadius();

            unit.Position = new Vector3(
                Mathf.Cos(angle) * distance,
                groundY - unit.GetLocalBottomY(),
                Mathf.Sin(angle) * distance);
        }
    }
}