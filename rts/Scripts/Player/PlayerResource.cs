using System;
using Godot;

namespace rts.scripts.Player;

[GlobalClass]
public partial class PlayerResource : Resource
{
    public enum PlayerResourceType
    {
        Gold,
        Wood,
        Stone,
        HexTile,
        Building,
        Unit
    }

    public event Action<float> ValueChanged;

    [Export] public PlayerResourceType Type { get; set; }

    [Export] public string DisplayName { get; set; } = string.Empty;

    [Export] public float Min { get; set; }

    [Export] public float Max { get; set; } = 100f;

    private float _value;

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

    public bool CanSubtract(float amount)
        => Value - amount >= Min;

    public bool TrySubtract(float amount)
    {
        if (amount <= 0f
            || !CanSubtract(amount))
            return false;

        Value -= amount;

        return true;
    }

    public bool CanAdd(float amount)
        => Value + amount <= Max;

    public bool TryAdd(float amount)
    {
        if (amount <= 0f ||
            !CanAdd(amount))
            return false;

        Value += amount;

        return true;
    }

    public bool CanIncrement()
        => CanAdd(1f);
    
    public bool TryIncrement()
        => TryAdd(1f);

    public bool CanDecrement()
        => CanSubtract(1f);
    
    public bool TryDecrement()
        => TrySubtract(1f);
}