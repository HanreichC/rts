using Godot;
using rts.Exceptions;
using rts.scripts.Player;

// ReSharper disable All

namespace rts.scripts.Buildings;

public partial class BuildingBase : Node3D
{
	[ExportGroup("Building Costs")] [Export] public PlayerRessourceType CostType { get; set; }

	[Export] public float CostValue { get; set; }

	private PlayerRessource _currentCostPlayerRessource => PlayerEconomy.Instance.TryGetPlayerRessource(CostType);
	
	[ExportGroup("Ressource Generation")] [Export] public PlayerRessourceType RessourceGenerationType { get; set; }
	
	[Export] public float RessourceGenerationValue { get; set; }
	[Export] public float RessourceGenerationInterval { get; set; }
	
	private PlayerRessource _currentRessourceGenerationPlayerRessource => PlayerEconomy.Instance.TryGetPlayerRessource(RessourceGenerationType);

	public void EnsureBuildRequirementsMet()
	{
		var currentPlayerRessource = _currentCostPlayerRessource;

		if (currentPlayerRessource is null
			|| !currentPlayerRessource.CanAfford(CostValue))
			throw new BuildRequirementsAreNotMetException();
	}

	public void SpendBuildRequirements()
		=> _currentCostPlayerRessource.Spend(CostValue);

	public override void _Ready()
	{
		if (_currentRessourceGenerationPlayerRessource == null
			|| RessourceGenerationValue <= 0
			|| RessourceGenerationInterval <= 0)
			return;
		
		var ressourceGenerationTimer = new Timer
		{
			WaitTime = RessourceGenerationInterval,
			Autostart = true
		};
		AddChild(ressourceGenerationTimer);
		ressourceGenerationTimer.Timeout += OnRessourceGenerationTimerTimeout;
	}
	
	private void OnRessourceGenerationTimerTimeout()
		=> _currentRessourceGenerationPlayerRessource.AddValue(RessourceGenerationValue);
}
