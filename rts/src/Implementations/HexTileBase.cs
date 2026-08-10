using Godot;
using rts.Helper;
using rts.Implementations;

namespace RTS.Implementations
{
	public partial class HexTileBase : Node3D
	{
		[Export] public PackedScene ConstructionSiteScene { get; set; }
		[Export] public float HexTileHeight { get; set; } = 1.0f;

		public int Q { get; set; } = 0;
		public int R { get; set; } = 0;

		public ConstructionSiteBase ConstructionSite { get; private set; }
		
		public Node3D Building { get; private set; }
		
		public bool AllowedToAddBuilding => Building == null;

		public override void _Ready()
		{
			if (ConstructionSiteScene != null)
			{
				ConstructionSite = ConstructionSiteScene.Instantiate<ConstructionSiteBase>();
				AddChild(ConstructionSite);
				ConstructionSite.Position = new Vector3(0, HexTileHeight / 2f, 0);

				SetConstructionSiteVisible(false);
			}
		}

		public void SetConstructionSiteVisible(bool visible)
		{
			if (ConstructionSite == null)
				return;

			if (!AllowedToAddBuilding)
				visible = false;

			ConstructionSite.Visible = visible;
		}
		
		public void AddBuilding(Node3D building)
		{
			if (building == null
			    || !AllowedToAddBuilding)
				return;

			Building = building;
			AddChild(Building);
			Building.Position = new Vector3(0, HexTileHeight / 2f, 0);
			Building.RotationDegrees = new Vector3(0, HexGridHelper.RandomRotation(), 0);
			SetConstructionSiteVisible(false);
		}
	}
}
