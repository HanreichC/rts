using RTS.Models;
using Godot;

public partial class GhostHexTile : HexTileBase
{
	[Export] public Godot.Collections.Array<PackedScene> HexTiles { get; set; } = new Godot.Collections.Array<PackedScene>();

	[Export] public
}
