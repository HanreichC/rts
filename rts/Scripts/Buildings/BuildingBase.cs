using Godot;
using rts.Exceptions;
using rts.scripts.Player;

namespace rts.scripts.Buildings;

public partial class BuildingBase : Node3D
{
	[ExportGroup("Costs")] [Export] public PlayerRessourceType CostType { get; set; }

	[Export] public float CostValue { get; set; }
	
	private PlayerRessource _currentPlayerRessource => PlayerEconomy.Instance.TryGetPlayerRessource(CostType);

	public void EnsureBuildRequirementsMet()
	{
		var currentPlayerRessource = _currentPlayerRessource;
		
		if (currentPlayerRessource is null
		    || !currentPlayerRessource.CanAfford(CostValue))
			throw new BuildRequirementsAreNotMetException();
	}

	public void SpendBuildRequirements()
		=> _currentPlayerRessource.Spend(CostValue);
}