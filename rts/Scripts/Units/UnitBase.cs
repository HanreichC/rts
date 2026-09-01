using Godot;
using rts.scripts.Buildings;

namespace rts.scripts.Units;

public partial class UnitBase : Node3DBase
{
    [ExportGroup("Movement")]
    [Export] public float MovementRadius { get; set; }
    
    public BuildingBase HomeBuilding { get; set; }
}