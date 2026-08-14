using Godot;
using rts.scripts.Player;

namespace rts.scripts;

public partial class Hud : CanvasLayer
{
	[Export] public Label GoldLabel { get; private set; }

	[Export] public Label WoodLabel { get; private set; }

	[Export] public Label StoneLabel { get; private set; }

	public override void _Ready()
	{
		PlayerEconomy.Instance.GoldRessource.AddRessourceValueChangedEventHandler(UpdateGoldLabel);
		PlayerEconomy.Instance.WoodRessource.AddRessourceValueChangedEventHandler(UpdateWoodLabel);
		PlayerEconomy.Instance.StoneRessource.AddRessourceValueChangedEventHandler(UpdateStoneLabel);
	}

	private void UpdateGoldLabel(float value)
		=> GoldLabel.Text = $"Gold: {value}/{PlayerEconomy.Instance.GoldRessource.MaxValue}";

	private void UpdateWoodLabel(float value)
		=> WoodLabel.Text = $"Wood: {value}/{PlayerEconomy.Instance.WoodRessource.MaxValue}";

	private void UpdateStoneLabel(float value)
		=> StoneLabel.Text = $"Stone: {value}/{PlayerEconomy.Instance.StoneRessource.MaxValue}";

	public override void _ExitTree()
	{
		PlayerEconomy.Instance.GoldRessource.RessourceValueChanged -= UpdateGoldLabel;
		PlayerEconomy.Instance.WoodRessource.RessourceValueChanged -= UpdateWoodLabel;
		PlayerEconomy.Instance.StoneRessource.RessourceValueChanged -= UpdateStoneLabel;
		
		base._ExitTree();
	}
}
