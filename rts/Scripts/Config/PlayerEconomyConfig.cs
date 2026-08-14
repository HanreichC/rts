using Godot;
using rts.scripts.Player;

namespace rts.scripts.Config;

[GlobalClass]
public partial class PlayerEconomyConfig : Resource
{
	[ExportGroup("Gold Player Ressource")]
	[Export] public string GoldName { get; set; }
	[Export] public float GoldCurrentValue { get; set; }
	[Export] public float GoldMaxValue { get; set; }
	[Export] public float GoldMinValue { get; set; }
	
	[ExportGroup("Wood Player Ressources")]
	[Export] public string WoodName { get; set; }
	[Export] public float WoodCurrentValue { get; set; }
	[Export] public float WoodMaxValue { get; set; }
	[Export] public float WoodMinValue { get; set; }
	
	[ExportGroup("Stone Player Ressources")]
	[Export] public string StoneName { get; set; }
	[Export] public float StoneCurrentValue { get; set; }
	[Export] public float StoneMaxValue { get; set; }
	[Export] public float StoneMinValue { get; set; }
}