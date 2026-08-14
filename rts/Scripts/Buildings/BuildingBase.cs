using Godot;
using rts.scripts.Player;

namespace rts.scripts.Buildings;

public partial class BuildingBase : Node3D
{
	[ExportGroup("Costs")] [Export] public PlayerRessourceType CostType { get; set; }

	[Export] public float CostValue { get; set; }

	public void EnsureBuildRequirementsMet()
	{
		
	}
}