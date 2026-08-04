using Godot;
using rts.Implementations;
using RTS.Implementations;
using System.Collections.Generic;

public partial class Main : Node3D
{
	[Export] public PackedScene HexScene { get; set; }
	[Export] public PackedScene GhostHexScene { get; set; }

	[Export] public float HexSize { get; set; } = 3.0f;

	[Export] public Node3D HexContainer { get; set; }
	[Export] public Camera3D Camera { get; set; }

	private readonly Dictionary<Vector2I, HexTileBase> _tiles = [];
	private readonly Dictionary<Vector2I, GhostHexTile> _ghostTiles = [];

	private bool _buildMode = false;
	private GhostHexTile _activePicker;

	public override void _Ready() => CreateStartTiles();

	private void CreateStartTiles()
	{
		Vector2I[] start =
		[
			new(0, 0), new(1, 0), new(0, 1), new(-1, 1),
			new(-1, 0), new(0, -1), new(1, -1)
		];
		foreach (var hex in start)
			AddHex(HexScene, hex);
	}

	private void AddHex(PackedScene hexScene, Vector2I key)
	{
		if (_tiles.ContainsKey(key))
			return;

		if (hexScene.Instantiate() is not HexTileBase hex)
		{
			GD.PrintErr("Scene root not HexTileBase");
			return;
		}

		hex.Position = HexGrid.AxialToWorld(key, HexSize);
		hex.Q = key.X;
		hex.R = key.Y;

		HexContainer.AddChild(hex);
		_tiles[key] = hex;

		if (_buildMode)
			hex.SetConstructionSiteVisible(true);
	}

	private void AddGhostHex(Vector2I key)
	{
		if (_tiles.ContainsKey(key) || _ghostTiles.ContainsKey(key))
			return;

		if (GhostHexScene.Instantiate() is not GhostHexTile ghost)
		{
			GD.PrintErr("GhostScene root not GhostHexTile");
			return;
		}

		ghost.Position = HexGrid.AxialToWorld(key, HexSize);
		ghost.Q = key.X;
		ghost.R = key.Y;

		HexContainer.AddChild(ghost);
		_ghostTiles[key] = ghost;
	}

	private void ClearGhostHexes()
	{
		ClosePicker();
		foreach (var ghost in _ghostTiles.Values)
			if (IsInstanceValid(ghost))
				ghost.QueueFree();
		_ghostTiles.Clear();
	}

	private void ToggleBuildMode()
	{
		_buildMode = !_buildMode;
		foreach (var tile in _tiles.Values)
			tile.SetConstructionSiteVisible(_buildMode);

		if (_buildMode)
			ShowBuildPositions();
		else
			ClearGhostHexes();
	}

	private void ShowBuildPositions()
	{
		ClearGhostHexes();

		foreach (var key in _tiles.Keys)
			foreach (var dir in HexGrid.Directions)
				AddGhostHex(key + dir); // AddGhostHex already de-dupes
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.B })
			ToggleBuildMode();

		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
			HandleLeftClick();
	}

	private void HandleLeftClick()
	{
		var hex = HexGrid.WorldToAxial(GetMouseWorldPointOnGround(), HexSize);

		if (_buildMode)
		{
			if (_ghostTiles.TryGetValue(hex, out GhostHexTile ghost))
				OpenPicker(ghost);
			else
				ClosePicker();
			return;
		}

		if (_tiles.TryGetValue(hex, out HexTileBase tile))
			GD.Print($"Clicked tile {hex} (Q={tile.Q}, R={tile.R})");
	}

	private void OpenPicker(GhostHexTile ghost)
	{
		if (_activePicker == ghost)
			return;

		ClosePicker();
		_activePicker = ghost;
		ghost.TileSelected += OnTileSelected;
		ghost.OpenPicker();
	}

	private void ClosePicker()
	{
		if (_activePicker == null)
			return;

		if (IsInstanceValid(_activePicker))
		{
			_activePicker.TileSelected -= OnTileSelected;
			_activePicker.ClosePicker();
		}
		_activePicker = null;
	}

	private void OnTileSelected(PackedScene scene)
	{
		if (_activePicker == null)
			return;

		var key = new Vector2I(_activePicker.Q, _activePicker.R);

		AddHex(scene, key);      // occupies the tile
		ShowBuildPositions();    // rebuilds ghosts -> old ghost cleaned up here
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

	public void OnBuildButtonPressed() => ToggleBuildMode();
}
