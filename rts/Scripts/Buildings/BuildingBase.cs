using Godot;
using rts.Exceptions;
using rts.scripts.Player;

namespace rts.scripts.Buildings;

public partial class BuildingBase : Node3D
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

    private PlayerResource CostResource => PlayerResources.Instance[CostType];

    private PlayerResource BuildingResource => PlayerResources.Instance[PlayerResource.PlayerResourceType.Building];

    private PlayerResource GenerationResource => PlayerResources.Instance[GenerationType];

    public void TryBuild(Vector3 position,
                         Vector3 rotation)
    {
        EnsureBuildRequirementsMet();
        SpendBuildRequirements();
        Position = position;
        RotationDegrees = rotation;
    }

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
}