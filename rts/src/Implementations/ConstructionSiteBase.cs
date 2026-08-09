using Godot;

namespace rts.Implementations;

public partial class ConstructionSiteBase : Node3D
{
    [Signal] public delegate void BuildingSelectedEventHandler(PackedScene scene);

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

    public override void _Process(double delta)
    {
        if (BuildingSelectionPanel == null
            || !BuildingSelectionPanel.Visible)
            return;

        var camera = GetViewport().GetCamera3D();
        if (camera == null)
            return;

        var screenPos = camera.UnprojectPosition(PlusLabel.GlobalPosition);
        BuildingSelectionPanel.Position = screenPos - new Vector2(BuildingSelectionPanel.Size.X / 2f, BuildingSelectionPanel.Size.Y + 10f);
    }

    public void OpenPicker() => BuildingSelectionPanel.Visible = true;
    public void ClosePicker() => BuildingSelectionPanel.Visible = false;

}