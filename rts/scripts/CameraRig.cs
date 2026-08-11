using Godot;

namespace rts.scripts;

/// <summary>
/// Orthografische RTS-Kamera.
///
/// Steuerung:
///   • WASD / Pfeiltasten           → schwenken (Pan)
///   • Q / E                        → drehen (Yaw)
///   • Mausrad                      → smooth zoomen
///   • Mittlere Maustaste + Ziehen  → schwenken (Pan)
///   • Rechte Maustaste + Ziehen    → drehen (Yaw)
///
/// Pitch ist bewusst fix: einmal einstellen im Inspector,
/// keine Live-Verstellung während des Spiels.
///
/// Szenenstruktur:
///   CameraRig (Node3D, dieses Script)  ← Pan + Yaw
///   └── PitchPivot (Node3D)             ← Fixed Pitch (aus PitchDegrees)
///       └── Camera3D (Orthogonal)       ← Position (0,0,Distance), Size = Zoom
/// </summary>
public partial class CameraRig : Node3D
{
	[ExportGroup("Pan")]
	[Export] public float KeyPanSpeed { get; set; } = 20f;
	[Export] public float MousePanSpeed { get; set; } = 0.035f;

	[ExportGroup("Rotation")]
	[Export] public float KeyYawSpeed { get; set; } = 1.8f;
	[Export] public float MouseYawSpeed { get; set; } = 0.005f;
	[Export(PropertyHint.Range, "-89,-10,1,degrees")]
	public float PitchDegrees { get; set; } = -55f;

	[ExportGroup("Zoom")]
	[Export] public float ZoomStep { get; set; } = 2f;
	[Export] public float MinZoom { get; set; } = 8f;
	[Export] public float MaxZoom { get; set; } = 80f;
	[Export] public float ZoomSmoothness { get; set; } = 12f;

	private Node3D _pitchPivot;
	private Camera3D _camera;
	private float _targetZoom;

	public override void _Ready()
	{
		_pitchPivot = GetNode<Node3D>("PitchPivot");
		_camera = GetNode<Camera3D>("PitchPivot/Camera3D");

		_camera.Projection = Camera3D.ProjectionType.Perspective;
		_pitchPivot.Rotation = new Vector3(Mathf.DegToRad(PitchDegrees), 0f, 0f);

		_targetZoom = Mathf.Clamp(_camera.Position.Z, MinZoom, MaxZoom);
		SetCameraDistance(_targetZoom);
	}

	public override void _Process(double dt)
	{
		float delta = (float)dt;
		UpdateKeyboardPan(delta);
		UpdateKeyboardYaw(delta);
		UpdateSmoothZoom(delta);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		switch (@event)
		{
			case InputEventMouseMotion motion when Input.IsMouseButtonPressed(MouseButton.Right):
				RotateY(-motion.ScreenRelative.X * MouseYawSpeed);
				break;

			case InputEventMouseMotion motion when Input.IsMouseButtonPressed(MouseButton.Middle):
				PanByScreen(motion.ScreenRelative, MousePanSpeed);
				break;

			case InputEventMouseButton mb when mb.Pressed && mb.ButtonIndex == MouseButton.WheelUp:
				_targetZoom = Mathf.Max(MinZoom, _targetZoom - ZoomStep);
				break;

			case InputEventMouseButton mb when mb.Pressed && mb.ButtonIndex == MouseButton.WheelDown:
				_targetZoom = Mathf.Min(MaxZoom, _targetZoom + ZoomStep);
				break;
		}
	}

	private void UpdateKeyboardPan(float delta)
	{
		Vector2 input = Vector2.Zero;
		if (Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up)) input.Y -= 1f;
		if (Input.IsKeyPressed(Key.S) || Input.IsKeyPressed(Key.Down)) input.Y += 1f;
		if (Input.IsKeyPressed(Key.A) || Input.IsKeyPressed(Key.Left)) input.X -= 1f;
		if (Input.IsKeyPressed(Key.D) || Input.IsKeyPressed(Key.Right)) input.X += 1f;

		if (input == Vector2.Zero) return;

		input = input.Normalized();
		(Vector3 right, Vector3 forward) = GetGroundBasis();
		Position += (right * input.X - forward * input.Y) * KeyPanSpeed * ZoomMultiplier() * delta;
	}

	private void UpdateKeyboardYaw(float delta)
	{
		float yaw = 0f;
		if (Input.IsKeyPressed(Key.Q)) yaw += 1f;
		if (Input.IsKeyPressed(Key.E)) yaw -= 1f;
		if (yaw == 0f) return;
		RotateY(yaw * KeyYawSpeed * delta);
	}

	private void UpdateSmoothZoom(float delta)
	{
		if (ZoomSmoothness <= 0f)
		{
			SetCameraDistance(_targetZoom);
			return;
		}
		float t = 1f - Mathf.Exp(-ZoomSmoothness * delta);
		SetCameraDistance(Mathf.Lerp(_camera.Position.Z, _targetZoom, t));
	}

	private void SetCameraDistance(float distance)
	{
		Vector3 position = _camera.Position;
		position.Z = distance;
		_camera.Position = position;
	}

	private void PanByScreen(Vector2 screenDelta, float speed)
	{
		(Vector3 right, Vector3 forward) = GetGroundBasis();
		Position += (-right * screenDelta.X + forward * screenDelta.Y) * speed * ZoomMultiplier();
	}

	// "Right" und "Forward" auf der horizontalen (X-Z) Ebene, entsprechend dem aktuellen Yaw.
	private (Vector3 right, Vector3 forward) GetGroundBasis()
	{
		Basis b = GlobalTransform.Basis;
		Vector3 right = new Vector3(b.X.X, 0f, b.X.Z).Normalized();
		Vector3 forward = new Vector3(-b.Z.X, 0f, -b.Z.Z).Normalized();
		return (right, forward);
	}

	// Weiter herausgezoomt = schnelleres Pan-Feeling.
	private float ZoomMultiplier() => Mathf.Clamp(_camera.Position.Z / 15f, 0.5f, 3f);
}
