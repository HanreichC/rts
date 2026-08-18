using System.Collections.Generic;
using Godot;
using rts.scripts.Config;

namespace rts.scripts.Player;

public partial class PlayerResources : Node
{
    private const string ConfigPath = "res://data/player_resources_config.tres";

    private readonly Dictionary<PlayerResource.PlayerResourceType, PlayerResource> _resources = new();

    public static PlayerResources Instance { get; private set; }

    public PlayerResource this[PlayerResource.PlayerResourceType type]
        => _resources.GetValueOrDefault(type);

    public override void _Ready()
    {
        Instance = this;

        var config = GD.Load<PlayerResourcesConfig>(ConfigPath);

        if (config is null)
        {
            GD.PushError($"Startkonfiguration konnte nicht geladen werden: {ConfigPath}");

            return;
        }

        foreach (var resource in config.Resources)
            _resources[resource.Type] = resource;
    }
}
