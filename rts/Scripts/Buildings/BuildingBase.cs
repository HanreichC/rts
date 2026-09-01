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

        SpawnUnits();
        
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
    
    private void SpawnUnits()                                        
    {                                                                
        if (UnitScene == null || UnitCount <= 0) return;             
                                                                   
        var groundY = GetLocalBottomY();                                              
                                                                   
        for (var i = 0; i < UnitCount; i++)                          
        {                                                            
            if (UnitScene.Instantiate() is not UnitBase unit)        
                continue;                                                        
                                                                   
            AddChild(unit);                   
            unit.HomeBuilding = this;                                
                                                                   
            var angle = Mathf.Tau * i / UnitCount;                   
            unit.Position = new Vector3(                             
                Mathf.Cos(angle) * UnitSpawnRadius,                  
                groundY - unit.GetLocalBottomY(),                    
                Mathf.Sin(angle) * UnitSpawnRadius);                 
        }                                                            
    }   
}