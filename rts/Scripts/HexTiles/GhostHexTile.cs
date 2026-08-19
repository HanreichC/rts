using Godot;
using rts.Helpers;

namespace rts.scripts.hexTiles;

public partial class GhostHexTile : HexTileBase
{
	[Signal] public delegate void TileSelectedEventHandler(PackedScene scene);

	[Export] public PackedScene GrassHexScene { get; set; }
	[Export] public PackedScene WaterHexScene { get; set; }

	[Export] public Label3D PlusLabel { get; set; }
	[Export] public PanelContainer HexTileSelectionPanel { get; set; }

	public override void _Ready()
	{
        base._Ready();

        if (HexTileSelectionPanel == null)
		{
			GD.PrintErr("GhostHexTile: HexTileSelectionPanel not assigned");
			return;
		}

		HexTileSelectionPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);
		HexTileSelectionPanel.Visible = false;
	
	}

	public void OpenPicker() => HexTileSelectionPanel.Visible = true;
	public void ClosePicker() => HexTileSelectionPanel.Visible = false;

	public override void _Process(double delta)
	{
		if (HexTileSelectionPanel == null
			|| !HexTileSelectionPanel.Visible)
			return;

		var camera = GetViewport().GetCamera3D();
		if (camera == null)
			return;

		var screenPos = camera.UnprojectPosition(PlusLabel.GlobalPosition);
		HexTileSelectionPanel.Position = screenPos - new Vector2(HexTileSelectionPanel.Size.X / 2f, HexTileSelectionPanel.Size.Y + 10f);
	}

	public override void TryPlace(Vector2I key, float hexSize)
	{
		Position = WorldHelper.AxialToWorld(key, hexSize);
		Q = key.X;
		R = key.Y;
	}

	public void OnGrassButtonPressed()
	{
		if (GrassHexScene != null)
			EmitSignal(SignalName.TileSelected, GrassHexScene);
	}

	public void OnWaterButtonPressed()
	{
		if (WaterHexScene != null)
			EmitSignal(SignalName.TileSelected, WaterHexScene);
	}
}
