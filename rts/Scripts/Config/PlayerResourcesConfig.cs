using Godot;
using Godot.Collections;
using rts.scripts.Player;

namespace rts.scripts.Config;

[GlobalClass]
public partial class PlayerResourcesConfig : Resource
{
    [Export] public Array<PlayerResource> Resources { get; set; } = [];
}
