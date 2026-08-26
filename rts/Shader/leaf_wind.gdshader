
shader_type spatial;

uniform vec4 leaf_color : source_color = vec4(0.08, 0.45, 0.05, 1.0);

uniform float wind_strength = 0.04;
uniform float wind_speed = 1.2;

void vertex() {
    float wave_x = sin(TIME * wind_speed + VERTEX.y);
    float wave_z = cos(TIME * wind_speed * 0.8 + VERTEX.x);

    VERTEX.x += wave_x * wind_strength;
    VERTEX.z += wave_z * wind_strength;
}

void fragment() {
    ALBEDO = leaf_color.rgb;
    ROUGHNESS = 0.85;
}