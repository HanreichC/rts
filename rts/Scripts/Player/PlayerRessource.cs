using Godot;

namespace rts.scripts.Player;

public enum PlayerRessourceType
{
    Gold,
    Wood,
    Stone
}

public partial class PlayerRessource : Resource
{
    public PlayerRessourceType Type { get; set; }
    
    [Export] public string Name { get; private set; }
    [Export] public float CurrentValue { get; private set; }
    [Export]public float MaxValue { get; private set; }
    [Export]public float MinValue { get; private set; } = 0;

    public void AddValue(float value)
    {
        var newValue = CurrentValue + value;
        CurrentValue = newValue > MaxValue ? MaxValue : newValue;
    }
    
    public bool CanAfford(float cost)
        => CurrentValue - cost >= MinValue;
    
    public void Spend(float cost)
    {
        if (!CanAfford(cost))
            return;
        
        CurrentValue -= cost;
    }
}