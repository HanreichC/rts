using System;
using Godot;

namespace rts.scripts.Player;

public enum PlayerResourceType
{
    Gold,
    Wood,
    Stone,
    HexTile,
    Building,
    Unit
}

[GlobalClass]
public partial class PlayerResource : Resource
{
    public event Action<float> ValueChanged;

    private float _value;

    [Export] public PlayerResourceType Type { get; set; }

    [Export] public string DisplayName { get; set; } = string.Empty;

    [Export] public float Min { get; set; }

    [Export] public float Max { get; set; } = 100f;

    [Export]
    public float Value
    {
        get => _value;
        set
        {
            var clamped = Mathf.Clamp(value, Min, Max);

            if (Mathf.IsEqualApprox(clamped, _value))
                return;

            _value = clamped;
            ValueChanged?.Invoke(_value);
        }
    }

    public bool CanAfford(float cost)
        => _value - cost >= Min;

    public void Add(float amount)
        => Value += amount;

    public bool TrySpend(float cost)
    {
        if (!CanAfford(cost))
            return false;

        Value -= cost;

        return true;
    }
}
