using System.Collections.Generic;
using System.Linq;
using Godot;
using rts.scripts.hexTiles;

namespace rts.scripts;

public partial class HexGrid : Node3D
{
    [Export] public PackedScene StartHexTileScene { get; set; }
    [Export] public PackedScene GhostHexTileScene { get; set; }

    private GhostHexTile _selectedGhostHexTile;

    [Export] public float HexSize { get; set; } = 3.0f;

    private readonly Dictionary<Vector2I, HexTileBase> _tiles = [];
    private readonly Dictionary<Vector2I, GhostHexTile> _ghostTiles = [];

    private bool _buildMode;

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

        var hex = InstantiateHexTile<HexTileBase>(hexScene, key);
        if (hex == null)
            return;

        _tiles[key] = hex;

        if (_buildMode)
            hex.SetConstructionSiteVisible(true);
    }

    private void AddGhostHexTile(Vector2I key)
    {
        if (_tiles.ContainsKey(key)
            || _ghostTiles.ContainsKey(key))
            return;

        var ghost = InstantiateHexTile<GhostHexTile>(GhostHexTileScene, key);
        if (ghost == null)
            return;

        _ghostTiles[key] = ghost;
    }

    // Shared placement logic for both real and ghost tiles: instantiate, position on the
    // hex grid, tag with its axial coordinates, and add as a child. Only the target
    // dictionary and any tile-type-specific follow-up differs between callers.
    private T InstantiateHexTile<T>(PackedScene scene, Vector2I key) where T : HexTileBase
    {
        if (scene.Instantiate() is not T tile)
        {
            GD.PrintErr($"Scene root not {typeof(T).Name}");
            return null;
        }

        tile.Position = AxialToWorld(key, HexSize);
        tile.Q = key.X;
        tile.R = key.Y;

        AddChild(tile);
        return tile;
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

        foreach (var key in _tiles.Keys)        // nur echte, gebaute Tiles als Ausgangspunkt
        foreach (var dir in neighborDirections)
            AddGhostHexTile(key + dir);          // AddGhostHexTile de-dupt bereits selbst
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

    private void OnHexTilePickerSelected(PackedScene scene)
    {
        if (_selectedGhostHexTile == null)
            return;

        var key = new Vector2I(_selectedGhostHexTile.Q, _selectedGhostHexTile.R);

        AddHexTile(scene, key);      // wird zur "echten" Tile -> landet in _tiles
        ShowBuildPositions();        // baut Ghosts anhand der jetzt aktualisierten _tiles neu auf
        CloseHexTilePicker();
    }

    public void OnBuildButtonPressed() => ToggleBuildMode();

    public void HandleClick(Vector3 worldPosition)
    {
        CloseHexTilePicker();
        foreach (var tile in _tiles)
            tile.Value.CloseConstructionSitePicker();
        
        var hex = WorldToAxial(worldPosition, HexSize);

        if (!_buildMode)
            return;

        if (_ghostTiles.TryGetValue(hex, out var selectedGhostHexTile))
        {
            OpenHexTilePicker(selectedGhostHexTile);
            return;
        }

        if (_tiles.TryGetValue(hex, out var selectedHexTile))
        {
            selectedHexTile.OpenConstructionSitePicker();
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