using System.Collections.Generic;
using Godot;
using rts.scripts.Config;

namespace rts.scripts.Player;

public partial class PlayerEconomy : Node
{
    private const string ConfigPath =
        "res://data/player_ressource_start_config.tres";

    public IEnumerable<PlayerRessource> PlayerRessources { get; private set; }

    public override void _Ready()
    {
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

        PlayerRessources =
        [
            config.Gold,
            config.Wood,
            config.Stone
        ];
    }
}