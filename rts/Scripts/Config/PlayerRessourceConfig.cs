using Godot;
using rts.scripts.Player;

namespace rts.scripts.Config;

[GlobalClass]
public partial class PlayerRessourceConfig : Resource
{
    [ExportGroup("Player Ressources")]
    [Export]
    public PlayerRessource Gold { get; set; } = new() { Type = PlayerRessourceType.Gold};
    
    [Export]
    public PlayerRessource Wood { get; set; } = new() { Type = PlayerRessourceType.Wood};
    
    [Export]
    public PlayerRessource Stone { get; set; } = new() { Type = PlayerRessourceType.Stone};
}