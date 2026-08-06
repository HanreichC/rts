using Godot;
using rts.Implementations;
using System;

namespace RTS.Implementations
{
	public partial class HexTileBase : Node3D
	{
		[Export] public PackedScene ConstructionSiteScene { get; set; }
		[Export] public float HexTileHeight { get; set; } = 1.0f;

		public int Q { get; set; } = 0;
		public int R { get; set; } = 0;

		public ConstructionSite ConstructionSite;
		public Node3D Building { get; private set; }

		public override void _Ready()
		{
			ConstructionSite = ConstructionSiteScene.Instantiate<ConstructionSite>();
			AddChild(ConstructionSite);
			ConstructionSite.Position = new Vector3(0, HexTileHeight / 2f, 0);

			SetConstructionSiteVisible(false);
		}

		public void SetConstructionSiteVisible(bool visible)
		{
			if (ConstructionSite == null)
				return;

			if (Building != null)
				visible = false;

			ConstructionSite.Visible = visible;
		}

		public void AddBuilding(Node3D building)
		{
			if (building == null)
				return;

			Building = building;
			AddChild(Building);
			Building.Position = new Vector3(0, HexTileHeight / 2f, 0);
			Building.RotationDegrees = new Vector3(0, HexGrid.RandomRotation(), 0);
			SetConstructionSiteVisible(false);
		}
	}
}
