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

	[Export] public float WaterHexTileYOffset { get; set; } = 0.5f;

	private readonly Dictionary<Vector2I, HexTileBase> _tiles = [];
	private readonly Dictionary<Vector2I, GhostHexTile> _ghostTiles = [];

	private bool _buildMode = false;

	private GhostHexTile _selectedGhostHexTile;
	private HexTileBase _selectedHexTile;

	public override void _Ready() => CreateStartTiles();

	private void CreateStartTiles()
	{
		Vector2I[] start =
		[
			new(0, 0), new(1, 0), new(0, 1), new(-1, 1),
			new(-1, 0), new(0, -1), new(1, -1)
		];
		foreach (var hex in start)
			AddHexTile(HexScene, hex);
	}

	private void AddHexTile(PackedScene hexScene, Vector2I key)
	{
		if (_tiles.ContainsKey(key))
			return;

		if (hexScene.Instantiate() is not HexTileBase hex)
		{
			GD.PrintErr("Scene root not HexTileBase");
			return;
		}

		var position = hex.Position = HexGrid.AxialToWorld(key, HexSize);


		if (hex is WaterHexTile)
			position.Y = WaterHexTileYOffset;

		hex.Position = position;
		hex.Q = key.X;
		hex.R = key.Y;

		HexContainer.AddChild(hex);
		_tiles[key] = hex;

		if (_buildMode)
			hex.SetConstructionSiteVisible(true);
	}

	private void AddBuilding(PackedScene buildingScene, Vector2I key)
	{
		if (!_tiles.TryGetValue(key, out HexTileBase hex))
		{
			GD.PrintErr($"No hex tile at {key} to add building to");
			return;
		}
		if (hex.ConstructionSite == null)
		{
			GD.PrintErr($"Hex tile at {key} has no construction site to add building to");
			return;
		}
		if (buildingScene.Instantiate() is not Node3D building)
		{
			GD.PrintErr("Building scene root not Node3D");
			return;
		}

		//building.Position = new Vector3(0, hex.HexTileHeight, 0);
		hex.AddBuilding(building);
		//hex.AddChild(building);
	}

	private void AddGhostHexTile(Vector2I key)
	{
		if (_tiles.ContainsKey(key)
			|| _ghostTiles.ContainsKey(key))
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

	private void ClearGhostHexTiles()
	{
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
			ClearGhostHexTiles();
	}

	private void ShowBuildPositions()
	{
		ClearGhostHexTiles();

		foreach (var key in _tiles.Keys)
			foreach (var dir in HexGrid.Directions)
				AddGhostHexTile(key + dir); // AddGhostHex already de-dupes
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
		CloseHexTilePicker();
		CloseConstructionSitePicker();

		var hex = HexGrid.WorldToAxial(GetMouseWorldPointOnGround(), HexSize);

		if (_buildMode)
		{
			if (_ghostTiles.TryGetValue(hex, out GhostHexTile selectedGhostHexTile))
				OpenHexTilePicker(selectedGhostHexTile);

			if (_tiles.TryGetValue(hex, out HexTileBase selectedHexTile))
				OpenConstructionSitePicker(selectedHexTile);

			return;
		}

		if (_tiles.TryGetValue(hex, out HexTileBase tile))
			GD.Print($"Clicked tile {hex} (Q={tile.Q}, R={tile.R})");
	}

	private void OpenHexTilePicker(GhostHexTile ghost)
	{
		if (_selectedGhostHexTile == ghost)
			return;

		_selectedGhostHexTile = ghost;
		ghost.TileSelected += OnHexTilePickerSelected;
		ghost.OpenPicker();
	}

	private void CloseHexTilePicker()
	{
		if (_selectedGhostHexTile == null)
			return;

		if (IsInstanceValid(_selectedGhostHexTile))
		{
			_selectedGhostHexTile.TileSelected -= OnHexTilePickerSelected;
			_selectedGhostHexTile.ClosePicker();
		}
		_selectedGhostHexTile = null;
	}

	private void OpenConstructionSitePicker(HexTileBase tile)
	{
		if (_selectedHexTile == tile)
			return;

		_selectedHexTile = tile;

		if (tile.ConstructionSite != null)
		{
			tile.ConstructionSite.BuildingSelected += OnBuildingPickerSelected;
			tile.ConstructionSite.OpenPicker();
		}
	}

	private void CloseConstructionSitePicker()
	{
		if (_selectedHexTile == null)
			return;

		if (_selectedHexTile.ConstructionSite != null)
		{
			_selectedHexTile.ConstructionSite.BuildingSelected -= OnBuildingPickerSelected;
			_selectedHexTile.ConstructionSite.ClosePicker();
		}

		_selectedHexTile = null;
	}

	private void OnHexTilePickerSelected(PackedScene scene)
	{
		GD.Print($"Selected {scene.ResourcePath.GetFile().GetBaseName()} at {_selectedGhostHexTile.Q}, {_selectedGhostHexTile.R}");

		if (_selectedGhostHexTile == null)
			return;

		var key = new Vector2I(_selectedGhostHexTile.Q, _selectedGhostHexTile.R);

		AddHexTile(scene, key);      // occupies the tile
		ShowBuildPositions();    // rebuilds ghosts -> old ghost cleaned up here
		CloseHexTilePicker();
	}

	private void OnBuildingPickerSelected(PackedScene scene)
	{
		GD.Print("picked");

		if (_selectedHexTile == null)
			return;

		var key = new Vector2I(_selectedHexTile.Q, _selectedHexTile.R);

		AddBuilding(scene, key);
		CloseConstructionSitePicker();
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
