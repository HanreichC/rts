using Godot;

namespace rts.scripts.constructionSites;

public partial class GrassConstructionSite : ConstructionSiteBase
{
	[Export] public PackedScene LumberjacksCabinScene { get; set; }

	public void OnLumberjacksCabinButtonPressed()
	{
		if (LumberjacksCabinScene != null)
			EmitSignal(ConstructionSiteBase.SignalName.BuildingSelected, LumberjacksCabinScene);
	}
}