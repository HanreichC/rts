shader_type spatial;

render_mode cull_disabled;

uniform vec4 water_color : source_color = vec4(0.0, 0.23, 0.78, 1.0);
uniform float wave_height : hint_range(0.0, 1.5, 0.01) = 0.65;
uniform float wave_speed : hint_range(0.0, 5.0, 0.01) = 2.2;
uniform float wave_density : hint_range(0.1, 6.0, 0.01) = 2.35;
uniform float turbulence : hint_range(0.0, 1.0, 0.01) = 0.92;
uniform float metallic : hint_range(0.0, 1.0, 0.01) = 0.15;
uniform float roughness : hint_range(0.0, 1.0, 0.01) = 0.34;

float hash(vec2 position) {
	return fract(sin(dot(position, vec2(127.1, 311.7))) * 43758.5453123);
}

float value_noise(vec2 position) {
	vec2 cell = floor(position);
	vec2 local = fract(position);
	local = local * local * (3.0 - 2.0 * local);

	float a = hash(cell);
	float b = hash(cell + vec2(1.0, 0.0));
	float c = hash(cell + vec2(0.0, 1.0));
	float d = hash(cell + vec2(1.0, 1.0));
	return mix(mix(a, b, local.x), mix(c, d, local.x), local.y);
}

float fractal_noise(vec2 position) {
	float value = 0.0;
	float amplitude = 0.5;
	mat2 rotation = mat2(vec2(0.80, -0.60), vec2(0.60, 0.80));

	for (int octave = 0; octave < 4; octave++) {
		value += value_noise(position) * amplitude;
		position = rotation * position * 2.03 + vec2(19.1, 7.7);
		amplitude *= 0.5;
	}

	return value / 0.9375;
}

float water_height(vec2 position, float time) {
	// Keep the sampled wave frequencies below the mesh's stable range.
	float stable_density = min(wave_density, 3.25);
	float stable_speed = min(wave_speed, 2.75);
	vec2 broad_flow = vec2(time * stable_speed * 0.035, -time * stable_speed * 0.022);
	float broad_waves = fractal_noise(position * stable_density * 0.10 + broad_flow) - 0.5;

	vec2 turbulence_flow = vec2(-time * stable_speed * 0.052, time * stable_speed * 0.037);
	vec2 turbulence_offset = vec2(
		fractal_noise(position * stable_density * 0.15 + turbulence_flow) - 0.5,
		fractal_noise(position * stable_density * 0.13 - turbulence_flow) - 0.5
	) * turbulence * 18.0;
	vec2 warped_position = position + turbulence_offset + vec2(broad_waves) * turbulence * 6.0;
	vec2 local_flow = vec2(-time * stable_speed * 0.068, time * stable_speed * 0.049);
	float local_waves = fractal_noise(warped_position * stable_density * 0.28 + local_flow) - 0.5;

	return broad_waves * 0.72 + local_waves * mix(0.24, 0.62, turbulence);
}

void vertex() {
	float height = water_height(VERTEX.xz, TIME);
	VERTEX.y += height * wave_height;
}

void fragment() {
	// Derivatives are constant across each rasterized triangle, yielding flat low-poly lighting.
	NORMAL = normalize(cross(dFdx(VERTEX), dFdy(VERTEX)));
	ALBEDO = water_color.rgb;
	METALLIC = metallic;
	SPECULAR = 0.85;
	ROUGHNESS = roughness;
}
