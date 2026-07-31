using RTS.Models;
using Godot;

public partial class GhostHexTile : HexTileBase
{
	[Signal] public delegate void TileSelectedEventHandler(PackedScene scene);

	[Export] public Godot.Collections.Array<PackedScene> HexTiles { get; set; } = new Godot.Collections.Array<PackedScene>();

	[Export] public Label3D PlusLabel { get; set; }

	[Export] public PanelContainer Container { get; set; }

	public override void _Ready()
	{
		if (Container == null)
		{
			GD.PrintErr("GhostHexTile: Container not assigned");
			return;
		}

		// Free positioning via _Process – anchors would fight the manual Position.
		Container.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);
		Container.Visible = false;
		BuildPicker();
	}

	public void OpenPicker() => Container.Visible = true;
	public void ClosePicker() => Container.Visible = false;

	public override void _Process(double delta)
	{
		if (Container == null || !Container.Visible) return;

		var camera = GetViewport().GetCamera3D();
		if (camera == null) return;

		var screenPos = camera.UnprojectPosition(PlusLabel.GlobalPosition);
		Container.Position = screenPos - new Vector2(Container.Size.X / 2f, Container.Size.Y + 10f);
	}

	[ExportGroup("Picker Layout")]
	[Export] public float PickerRadius { get; set; } = 80f;
	[Export] public float PickerButtonSize { get; set; } = 64f;
	[Export] public int PreviewResolution { get; set; } = 128;

	private void BuildPicker()
	{
		foreach (var child in Container.GetChildren()) child.QueueFree();
		if (HexTiles.Count == 0) return;

		float wheelSize = (PickerRadius + PickerButtonSize / 2f) * 2f + 16f;
		var wheel = new Control
		{
			CustomMinimumSize = new Vector2(wheelSize, wheelSize)
		};
		Container.AddChild(wheel);

		Vector2 center = new(wheelSize / 2f, wheelSize / 2f);
		int count = HexTiles.Count;

		for (int i = 0; i < count; i++)
		{
			var scene = HexTiles[i];
			float angle = Mathf.Tau * i / count - Mathf.Pi / 2f; // start at top, go clockwise
			Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * PickerRadius;

			var btn = new TextureButton
			{
				TextureNormal = RenderPreview(scene),
				IgnoreTextureSize = true,
				StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
				Size = new Vector2(PickerButtonSize, PickerButtonSize),
				Position = pos - new Vector2(PickerButtonSize / 2f, PickerButtonSize / 2f),
				PivotOffset = new Vector2(PickerButtonSize / 2f, PickerButtonSize / 2f),
				TooltipText = scene.ResourcePath.GetFile().GetBaseName()
			};

			// subtle hover feedback
			btn.MouseEntered += () => btn.Scale = new Vector2(1.15f, 1.15f);
			btn.MouseExited += () => btn.Scale = Vector2.One;

			var captured = scene;
			btn.Pressed += () => EmitSignal(SignalName.TileSelected, captured);

			wheel.AddChild(btn);
		}
	}

	// Renders a tile scene into a texture via an isolated 3D SubViewport.
	private Texture2D RenderPreview(PackedScene scene)
	{
		var sv = new SubViewport
		{
			Size = new Vector2I(PreviewResolution, PreviewResolution),
			TransparentBg = true,
			OwnWorld3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always
		};
		AddChild(sv);

		var tile = scene.Instantiate<Node3D>();
		sv.AddChild(tile);

		var cam = new Camera3D
		{
			Projection = Camera3D.ProjectionType.Orthogonal,
			Size = 7f,
			Position = new Vector3(0, 6, 6)
		};
		sv.AddChild(cam);
		cam.LookAt(Vector3.Zero, Vector3.Up);
		cam.Current = true;

		var light = new DirectionalLight3D
		{
			RotationDegrees = new Vector3(-50, -35, 0)
		};
		sv.AddChild(light);

		return sv.GetTexture();
	}
}
