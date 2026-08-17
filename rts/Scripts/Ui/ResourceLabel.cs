using Godot;
using rts.scripts.Player;

namespace rts.scripts.Ui;

public partial class ResourceLabel : Label
{
    private PlayerResource _resource;

    [Export] public PlayerResourceType Type { get; set; }

    public override void _Ready()
    {
        _resource = PlayerResources.Instance[Type];

        if (_resource is null)
            return;

        _resource.ValueChanged += OnValueChanged;
        OnValueChanged(_resource.Value);
    }

    public override void _ExitTree()
    {
        if (_resource is not null)
            _resource.ValueChanged -= OnValueChanged;

        base._ExitTree();
    }

    private void OnValueChanged(float value)
        => Text = $"{_resource.DisplayName}: {value:0}/{_resource.Max:0}";
}
