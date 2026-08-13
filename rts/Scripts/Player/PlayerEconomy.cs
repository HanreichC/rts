using System.Collections.Generic;
using Godot;
using rts.scripts.Config;

namespace rts.scripts.Player;

public partial class PlayerEconomy : Node
{
    public static PlayerEconomy Instance { get; private set; }
    
    private const string ConfigPath =
        "res://data/player_ressource_start_config.tres";

    public PlayerRessource GoldRessource { get; private set; }
    
    public PlayerRessource WoodRessource { get; private set; }
    
    public PlayerRessource StoneRessource { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        
        LoadStartValues();
    }

    private void LoadStartValues()
    {
        var config = GD.Load<PlayerRessourceConfig>(ConfigPath);

        if (config == null)
        {
            GD.PushError(
                "Startkonfiguration konnte nicht geladen werden: "
                + ConfigPath
            );

            return;
        }
        
        GoldRessource =
            new PlayerRessource(
                PlayerRessourceType.Gold,
                config.GoldName,
                config.GoldCurrentValue,
                config.GoldMaxValue,
                config.GoldMinValue);
        
        WoodRessource = 
            new PlayerRessource(
                PlayerRessourceType.Wood,
                config.WoodName,
                config.WoodCurrentValue,
                config.WoodMaxValue,
                config.WoodMinValue);
            
        StoneRessource = 
            new PlayerRessource(
                PlayerRessourceType.Stone,
                config.StoneName,
                config.StoneCurrentValue,
                config.StoneMaxValue,
                config.StoneMinValue);
    }
}