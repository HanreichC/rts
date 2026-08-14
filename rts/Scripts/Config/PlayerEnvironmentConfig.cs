using Godot;

namespace rts.scripts.Config;

[GlobalClass]
public partial class PlayerEnvironmentConfig : Resource
{
    [ExportGroup("HexTile Player Environment")]
    [Export] public string HexTileName { get; set; }
    [Export] public float HexTileCurrentValue { get; set; }
    [Export] public float HexTileMaxValue { get; set; }
    [Export] public float HexTileMinValue { get; set; }
	
    [ExportGroup("Building Player Environment")]
    [Export] public string BuildingName { get; set; }
    [Export] public float BuildingCurrentValue { get; set; }
    [Export] public float BuildingMaxValue { get; set; }
    [Export] public float BuildingMinValue { get; set; }
	
    [ExportGroup("Unit Player Environment")]
    [Export] public string UnitName { get; set; }
    [Export] public float UnitCurrentValue { get; set; }
    [Export] public float UnitMaxValue { get; set; }
    [Export] public float UnitMinValue { get; set; }
}