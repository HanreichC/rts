using Godot;
using rts.Implementations;
// ReSharper disable All

public partial class Main : Node3D
{
	[Export] public Camera3D Camera { get; set; }
	
	[Export]
	public PackedScene HexGridScene { get; set; }
	
	private HexGrid _hexGrid;
	
	public override void _Ready()
	{
		if (HexGridScene != null)
		{
			_hexGrid = HexGridScene.Instantiate<HexGrid>();
			AddChild(_hexGrid);
		}
	}
	
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.B })
			_hexGrid.ToggleBuildMode();

		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			_hexGrid.HandleClick(GetMouseWorldPointOnGround());
	}

	// Y=0 ground plane math is enough for a flat grid — no physics query needed.
	private Vector3 GetMouseWorldPointOnGround()
	{
		var mouse = GetViewport().GetMousePosition();
		var origin = Camera.ProjectRayOrigin(mouse);
		var dir = Camera.ProjectRayNormal(mouse);

		var ground = new Plane(Vector3.Up, 0f);
		return ground.IntersectsRay(origin, dir) ?? Vector3.Zero;
	}
}