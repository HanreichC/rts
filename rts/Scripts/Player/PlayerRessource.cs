using System;
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
    [Signal] public delegate void RessourceValueChangedEventHandler(float newCurrentValue);
    
    public PlayerRessourceType Type { get; set; }
    
    [Export] public string Name { get; private set; }
    [Export] public float CurrentValue { get; private set; }
    [Export]public float MaxValue { get; private set; }
    [Export]public float MinValue { get; private set; } = 0;

    public PlayerRessource()
    {
    }
    
    public PlayerRessource(PlayerRessourceType type) : this()
        => Type = type;

    public PlayerRessource(
        PlayerRessourceType type,
        string name, 
        float currentValue,
        float maxValue,
        float minValue)
    : this()
    {
        Type = type;
        Name = name;
        CurrentValue = currentValue;
        MaxValue = maxValue;
        MinValue = minValue;
        
        EmitSignal(SignalName.RessourceValueChanged, CurrentValue);
    }


    public void AddValue(float value)
    {
        var newValue = CurrentValue + value;
        CurrentValue = newValue > MaxValue ? MaxValue : newValue;
        EmitSignal(SignalName.RessourceValueChanged, CurrentValue);
    }
    
    public bool CanAfford(float cost)
        => CurrentValue - cost >= MinValue;
    
    public void Spend(float cost)
    {
        if (!CanAfford(cost))
            return;
        
        CurrentValue -= cost;
        EmitSignal(SignalName.RessourceValueChanged, CurrentValue);
    }

    public void AddRessourceValueChangedEventHandler(Action<float> handler)
    {
        RessourceValueChanged += new RessourceValueChangedEventHandler(handler);
        EmitSignal(SignalName.RessourceValueChanged, CurrentValue);
    }
}