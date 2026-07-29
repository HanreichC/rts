using Godot;
using RTS.Models;
using System.Collections.Generic;

public partial class Main : Node3D
{
	[Export] public PackedScene HexScene { get; set; }
	[Export] public PackedScene GhostHexScene { get; set; }

	[Export] public float HexSize { get; set; } = 3.0f;

	private Node3D _hexContainer;
	private Camera3D _camera;

	private readonly Dictionary<Vector2I, HexTileBase> _tiles = [];
	private readonly Dictionary<Vector2I, GhostHexTile> _ghostTiles = [];

	private bool _buildMode = false;

	public override void _Ready()
	{
		GD.Print("Main._Ready()");
		_hexContainer = GetNode<Node3D>("HexContainer");
		_camera = GetNode<Camera3D>("CameraRig/PitchPivot/Camera3D");

		if (_hexContainer == null) { GD.PrintErr("HexContainer missing"); return; }
		if (HexScene == null) { GD.PrintErr("HexScene missing"); return; }
		if (GhostHexScene == null) { GD.PrintErr("GhostHexScene missing"); return; }

		CreateStartTiles();
	}

	private void CreateStartTiles()
	{
		AddHex(0, 0);
		AddHex(1, 0);
		AddHex(0, 1);
		AddHex(-1, 1);
		AddHex(-1, 0);
		AddHex(0, -1);
		AddHex(1, -1);
	}

	private void AddHex(int q, int r)
	{
		var key = new Vector2I(q, r);
		if (_tiles.ContainsKey(key)) return;

		Node node = HexScene.Instantiate();
		if (node is not HexTile hex) { GD.PrintErr("HexScene root not HexTile"); return; }

		hex.Position = AxialToWorld(q, r);
		hex.Q = q; hex.R = r;
		_hexContainer.AddChild(hex);
		_tiles[key] = hex;
	}

	private void AddGhostHex(int q, int r)
	{
		var key = new Vector2I(q, r);
		if (_tiles.ContainsKey(key)) return;
		if (_ghostTiles.ContainsKey(key)) return;

		Node node = GhostHexScene.Instantiate();
		if (node is not GhostHexTile ghost) { GD.PrintErr("GhostScene root not GhostHexTile"); return; }

		ghost.Position = AxialToWorld(q, r);
		ghost.Q = q; ghost.R = r;
		_hexContainer.AddChild(ghost);
		_ghostTiles[key] = ghost;
	}

	private void ClearGhostHexes()
	{
		foreach (var pair in _ghostTiles)
			if (IsInstanceValid(pair.Value))
				pair.Value.QueueFree();
		_ghostTiles.Clear();
	}
	private void ToggleBuildMode()
	{
		_buildMode = !_buildMode;
		if (_buildMode) ShowBuildPositions(); else ClearGhostHexes();
	}

	private void ShowBuildPositions()
	{
		ClearGhostHexes();

		var checkeds = new HashSet<Vector2I>();
		foreach (var k in _tiles.Keys)
		{
			int q = k.X, r = k.Y;
			Vector2I[] neighbors =
			[
				new(q + 1, r),
				new(q, r + 1),
				new(q - 1, r + 1),
				new(q - 1, r),
				new(q, r - 1),
				new(q + 1, r - 1)
			];

			foreach (var n in neighbors)
			{
				if (_tiles.ContainsKey(n)) continue;
				if (checkeds.Contains(n)) continue;
				checkeds.Add(n);
				AddGhostHex(n.X, n.Y);
			}
		}
	}

	private Vector3 AxialToWorld(int q, int r)
	{
		float x = HexSize * Mathf.Sqrt(3.0f) * (q + r / 2.0f);
		float z = HexSize * 1.5f * r;
		return new Vector3(x, 0f, z);
	}

	// converts a world point to axial coords (rounded)
	private Vector2I WorldToAxial(Vector3 worldPos)
	{
		// inverse of AxialToWorld for flat-top hexes (matching AxialToWorld above)
		float qf = (Mathf.Sqrt(3f) / 3f * worldPos.X - 1f / 3f * worldPos.Z) / HexSize;
		float rf = (2f / 3f * worldPos.Z) / HexSize;
		return CubeRound(qf, rf);
	}

	private Vector2I CubeRound(float qf, float rf)
	{
		float sf = -qf - rf;
		int q = Mathf.RoundToInt(qf);
		int r = Mathf.RoundToInt(rf);
		int s = Mathf.RoundToInt(sf);

		float qdiff = Mathf.Abs(q - qf);
		float rdiff = Mathf.Abs(r - rf);
		float sdiff = Mathf.Abs(s - sf);

		if (qdiff > rdiff && qdiff > sdiff) q = -r - s;
		else if (rdiff > sdiff) r = -q - s;

		return new Vector2I(q, r);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.B) ToggleBuildMode();
		}

		if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed)
		{
			if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
				HandleLeftClick();
		}
	}

	private void HandleLeftClick()
	{
		// Project mouse onto ground plane Y=0 — robust for orthographic & perspective
		Vector3 hit = GetMouseWorldPointOnGround();
		Vector2I hex = WorldToAxial(hit);

		if (_buildMode)
		{
			if (_ghostTiles.TryGetValue(hex, out GhostHexTile ghost))
			{
				AddHex(ghost.Q, ghost.R);
				if (IsInstanceValid(ghost)) ghost.QueueFree();
				_ghostTiles.Remove(hex);
				ShowBuildPositions();
			}
		}
		else
		{
			// not build mode: you can select tiles by coordinate
			if (_tiles.TryGetValue(hex, out HexTileBase t))
			{
				GD.Print($"Clicked tile {hex} (Q={t.Q}, R={t.R})");
			}
		}
	}

	private Vector3 GetMouseWorldPointOnGround()
	{
		Vector2 mouse = GetViewport().GetMousePosition();
		Vector3 origin = _camera.ProjectRayOrigin(mouse);
		Vector3 dir = _camera.ProjectRayNormal(mouse);

		Plane ground = new Plane(Vector3.Up, 0f); // Y=0
		Vector3? intersect = ground.IntersectsRay(origin, dir);
		if (intersect.HasValue) return intersect.Value;
		// fallback: raycast into world
		PhysicsDirectSpaceState3D ss = GetWorld3D().DirectSpaceState;
		var q = PhysicsRayQueryParameters3D.Create(origin, origin + dir * 1000f);
		var res = ss.IntersectRay(q);
		if (res.Count > 0 && res.ContainsKey("position")) return (Vector3)res["position"];
		return Vector3.Zero;
	}

	public void OnBuildButtonPressed() => ToggleBuildMode();
}
