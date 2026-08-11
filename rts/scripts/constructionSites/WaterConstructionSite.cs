using Godot;

namespace rts.scripts.constructionSites;

public partial class WaterConstructionSite : ConstructionSiteBase
{
	[Export] public PackedScene FishermansCabinScene { get; set; }

	public void OnFishermansCabinButtonPressed()
	{
		if (FishermansCabinScene != null)
			EmitSignal(ConstructionSiteBase.SignalName.BuildingSelected, FishermansCabinScene);
	}
}
