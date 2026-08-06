using Godot;

namespace RTS.Implementations
{
	public partial class HexTileBase : Node3D
	{
		[Export] public PackedScene ConstructionSiteScene { get; set; }
		[Export] public float ConstructionSiteHeight { get; set; } = 0.5f;

		public int Q { get; set; } = 0;
		public int R { get; set; } = 0;

		public ConstructionSite ConstructionSite;

		public override void _Ready()
        {
            ConstructionSite = ConstructionSiteScene.Instantiate<ConstructionSite>();
            AddChild(ConstructionSite);
            ConstructionSite.Position = new Vector3(0, ConstructionSiteHeight, 0);

			SetConstructionSiteVisible(false);
        }

        public void SetConstructionSiteVisible(bool visible)
		{
			if (ConstructionSite == null)
				return;

			ConstructionSite.Visible = visible;
		}
	}
}
