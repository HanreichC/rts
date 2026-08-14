using Godot;

namespace rts.scripts.Player;

public partial class PlayerEnvironment : Node
{
    public static PlayerEnvironment Instance { get; private set; }
    
    private const string ConfigPath =
        "res://data/player_environment_start_config.tres";
}