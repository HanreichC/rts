using System.Collections.Generic;
using System.Linq;
using Godot;
using rts.scripts.Buildings;
using rts.scripts.hexTiles;

namespace rts.scripts;

public partial class HexGrid : Node3D
{
	[Export] public PackedScene StartHexTileScene { get; set; }
	[Export] public PackedScene GhostHexTileScene { get; set; }
	
	private GhostHexTile _selectedGhostHexTile;
	private HexTileBase _selectedHexTile;
	
	[Export] public float HexSize { get; set; } = 3.0f;

	private readonly Dictionary<Vector2I, HexTileBase> _tiles = [];
	private readonly Dictionary<Vector2I, GhostHexTile> _ghostTiles = [];
	
	private bool _buildMode = false;
	
	public override void _Ready()
	{
		CreateStartTiles();
	}
	
	private void CreateStartTiles()
	{
		Vector2I[] start =
		[
			new(0, 0), new(1, 0), new(0, 1), new(-1, 1),
			new(-1, 0), new(0, -1), new(1, -1)
		];
		foreach (var hex in start)
			AddHexTile(StartHexTileScene, hex);
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

		var position = hex.Position = AxialToWorld(key, HexSize);

		hex.Position = position;
		hex.Q = key.X;
		hex.R = key.Y;

		AddChild(hex);
		_tiles[key] = hex;

		if (_buildMode)
			hex.SetConstructionSiteVisible(true);
	}
	
	private void AddBuilding(PackedScene buildingScene, Vector2I key)
	{
		if (!_tiles.TryGetValue(key, out var hex))
		{
			GD.PrintErr($"No hex tile at {key} to add building to");
			return;
		}
		if (hex.ConstructionSite == null)
		{
			GD.PrintErr($"Hex tile at {key} has no construction site to add building to");
			return;
		}
		if (buildingScene.Instantiate() is not BuildingBase building)
		{
			GD.PrintErr("Building scene root not Node3D");
			return;
		}
		
		hex.AddBuilding(building);
	}

	private void AddGhostHexTile(Vector2I key)
	{
		if (_tiles.ContainsKey(key)
			|| _ghostTiles.ContainsKey(key))
			return;

		if (GhostHexTileScene.Instantiate() is not GhostHexTile ghost)
		{
			GD.PrintErr("GhostScene root not GhostHexTile");
			return;
		}

		ghost.Position = AxialToWorld(key, HexSize);
		ghost.Q = key.X;
		ghost.R = key.Y;

		AddChild(ghost);
		_ghostTiles[key] = ghost;
	}

	private void ClearGhostHexTiles()
	{
		foreach (var ghost in _ghostTiles.Values.Where(IsInstanceValid))
			ghost.QueueFree();
		_ghostTiles.Clear();
	}

	public void ToggleBuildMode()
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

		Vector2I[] neighborDirections =
		[
			new(1, 0), new(0, 1), new(-1, 1),
			new(-1, 0), new(0, -1), new(1, -1)
		];
		
		foreach (var key in _tiles.Keys)
		foreach (var dir in neighborDirections)
			AddGhostHexTile(key + dir); // AddGhostHex already de-dupes
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

		if (tile.ConstructionSite == null
			|| !tile.AllowedToAddBuilding)
			return;
		
		tile.ConstructionSite.BuildingSelected += OnBuildingPickerSelected;
		tile.ConstructionSite.OpenPicker();
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
		if (_selectedHexTile == null)
			return;

		var key = new Vector2I(_selectedHexTile.Q, _selectedHexTile.R);

		AddBuilding(scene, key);
		CloseConstructionSitePicker();
	}
	
	public void OnBuildButtonPressed() => ToggleBuildMode();

	public void HandleClick(Vector3 worldPosition)
	{
		CloseHexTilePicker();
		CloseConstructionSitePicker();

		var hex = WorldToAxial(worldPosition, HexSize);

		if (_buildMode)
		{
			if (_ghostTiles.TryGetValue(hex, out var selectedGhostHexTile))
				OpenHexTilePicker(selectedGhostHexTile);

			if (_tiles.TryGetValue(hex, out var selectedHexTile))
				OpenConstructionSitePicker(selectedHexTile);

			return;
		}
	}
	
	private static Vector3 AxialToWorld(Vector2I hex, float hexSize)
	{
		var x = hexSize * Mathf.Sqrt(3.0f) * (hex.X + hex.Y / 2.0f);
		var z = hexSize * 1.5f * hex.Y;
		return new Vector3(x, 0f, z);
	}

	private static Vector2I WorldToAxial(Vector3 worldPos, float hexSize)
	{
		var qf = (Mathf.Sqrt(3f) / 3f * worldPos.X - 1f / 3f * worldPos.Z) / hexSize;
		var rf = (2f / 3f * worldPos.Z) / hexSize;
		return CubeRound(qf, rf);
	}

	private static Vector2I CubeRound(float qf, float rf)
	{
		var sf = -qf - rf;
		var q = Mathf.RoundToInt(qf);
		var r = Mathf.RoundToInt(rf);
		var s = Mathf.RoundToInt(sf);

		var qdiff = Mathf.Abs(q - qf);
		var rdiff = Mathf.Abs(r - rf);
		var sdiff = Mathf.Abs(s - sf);

		if (qdiff > rdiff && qdiff > sdiff) q = -r - s;
		else if (rdiff > sdiff) r = -q - s;

		return new Vector2I(q, r);
	}
}
