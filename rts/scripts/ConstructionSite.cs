using Godot;

public partial class ConstructionSite : Node3D
{
	[Signal] public delegate void BuildingSelectedEventHandler(PackedScene scene);

	[Export] public PackedScene HouseScene { get; set; }

	[Export] public PanelContainer BuildingSelectionPanel { get; set; }
	[Export] public Label3D PlusLabel { get; set; }

	public override void _Ready()
	{
		if (BuildingSelectionPanel == null)
		{
			GD.PrintErr("ConstructionSite: BuildingSelectionPanel not assigned");
			return;
		}

		BuildingSelectionPanel.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);
		BuildingSelectionPanel.Visible = false;
	}

	public void OpenPicker() => BuildingSelectionPanel.Visible = true;
	public void ClosePicker() => BuildingSelectionPanel.Visible = false;

	public void OnHouseButtonPressed()
	{
		if (HouseScene != null)
			EmitSignal(SignalName.BuildingSelected, HouseScene);
	}
}
