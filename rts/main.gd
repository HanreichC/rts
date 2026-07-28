extends Node3D

@onready var hex_container: Node3D = $HexContainer
@onready var yaw_pivot: Node3D = $CameraRig/YawPivot
@onready var pitch_pivot: Node3D = $CameraRig/YawPivot/PitchPivot
@onready var spring_arm: SpringArm3D = $CameraRig/YawPivot/PitchPivot/SpringArm3D
@onready var camera: Camera3D = $CameraRig/YawPivot/PitchPivot/SpringArm3D/Camera3D

var hex_scene := preload("res://hex_tile.tscn")
var tiles := {}

const HEX_SIZE := 1.0
const CAMERA_ROT_SPEED := 0.01
const CAMERA_ZOOM_STEP := 1.0
const CAMERA_MIN_ZOOM := 4.0
const CAMERA_MAX_ZOOM := 20.0

func _ready():
	create_start_tiles()

func create_start_tiles():
	add_hex(0, 0)
	add_hex(1, 0)
	add_hex(0, 1)
	add_hex(-1, 1)
	add_hex(-1, 0)
	add_hex(0, -1)
	add_hex(1, -1)

func add_hex(q: int, r: int):
	var key = Vector2i(q, r)
	if tiles.has(key):
		return

	var hex = hex_scene.instantiate()
	var world_pos = axial_to_world(q, r)

	hex.position = world_pos
	hex.q = q
	hex.r = r

	hex_container.add_child(hex)
	tiles[key] = hex

func axial_to_world(q: int, r: int) -> Vector3:
	var x = HEX_SIZE * sqrt(3.0) * (q + r / 2.0)
	var z = HEX_SIZE * 1.5 * r
	return Vector3(x, 0, z)

func _unhandled_input(event):
	if event is InputEventMouseMotion and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
		yaw_pivot.rotate_y(-event.screen_relative.x * CAMERA_ROT_SPEED)

		pitch_pivot.rotation.x -= event.screen_relative.y * CAMERA_ROT_SPEED
		pitch_pivot.rotation.x = clamp(pitch_pivot.rotation.x, deg_to_rad(-80), deg_to_rad(-20))

	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_WHEEL_UP:
			spring_arm.spring_length = max(CAMERA_MIN_ZOOM, spring_arm.spring_length - CAMERA_ZOOM_STEP)

		if event.button_index == MOUSE_BUTTON_WHEEL_DOWN:
			spring_arm.spring_length = min(CAMERA_MAX_ZOOM, spring_arm.spring_length + CAMERA_ZOOM_STEP)

		if event.button_index == MOUSE_BUTTON_LEFT:
			try_place_hex()

func try_place_hex():
	var result = raycast_from_mouse()
	if result.is_empty():
		return

	var collider = result["collider"]
	if collider == null:
		return

	var tile_node = collider.get_parent()
	if tile_node == null:
		return

	if not "q" in tile_node or not "r" in tile_node:
		return

	var base_q = tile_node.q
	var base_r = tile_node.r

	var neighbors = [
		Vector2i(base_q + 1, base_r),
		Vector2i(base_q, base_r + 1),
		Vector2i(base_q - 1, base_r + 1),
		Vector2i(base_q - 1, base_r),
		Vector2i(base_q, base_r - 1),
		Vector2i(base_q + 1, base_r - 1)
	]

	for neighbor in neighbors:
		if not tiles.has(neighbor):
			add_hex(neighbor.x, neighbor.y)
			return

func raycast_from_mouse() -> Dictionary:
	var mouse_pos = get_viewport().get_mouse_position()
	var ray_origin = camera.project_ray_origin(mouse_pos)
	var ray_end = ray_origin + camera.project_ray_normal(mouse_pos) * 1000.0

	var space_state = get_world_3d().direct_space_state
	var query = PhysicsRayQueryParameters3D.create(ray_origin, ray_end)
	return space_state.intersect_ray(query)
