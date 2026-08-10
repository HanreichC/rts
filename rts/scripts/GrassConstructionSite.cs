using Godot;
using rts.Implementations;

public partial class GrassConstructionSite : ConstructionSiteBase
{
	[Export] public PackedScene HouseScene { get; set; }

	public void OnHouseButtonPressed()
	{
		if (HouseScene != null)
			EmitSignal(ConstructionSiteBase.SignalName.BuildingSelected, HouseScene);
	}
}