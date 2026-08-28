shader_type spatial;

// Wind fuer Low-Poly-Blattcluster (Icospheres).
// Jeder Cluster neigt sich als Ganzes um einen Drehpunkt unter sich, statt sich
// zu verformen. Die Phase kommt aus der Weltposition des Clusters, dadurch
// laeuft eine Welle durch die Krone, statt dass alles synchron pulsiert.

uniform vec4 leaf_color : source_color = vec4(0.15294118, 0.48235294, 0.19215687, 1.0);

group_uniforms Wind;
// Windrichtung in der XZ-Ebene (wird normalisiert).
uniform vec2 wind_direction = vec2(1.0, 0.35);
uniform float wind_strength : hint_range(0.0, 0.3) = 0.045;
uniform float wind_speed : hint_range(0.0, 4.0) = 0.55;
// Laenge einer Windwelle in Metern: wie weit Cluster zeitlich versetzt schwingen.
uniform float wind_wavelength : hint_range(1.0, 60.0) = 14.0;
// Boeen: langsames An- und Abschwellen, damit es nicht metronomisch wirkt.
uniform float gust_amount : hint_range(0.0, 1.0) = 0.45;
uniform float gust_speed : hint_range(0.0, 1.0) = 0.13;
// Drehpunkt unterhalb des Clustermittelpunkts. Groesser = mehr Versatz,
// weniger Neigung (Cluster driftet eher, als dass er kippt).
uniform float pivot_offset : hint_range(0.0, 8.0) = 2.0;
// Pro Cluster nachjustierbar, ohne den Rest anzufassen.
uniform float sway_scale : hint_range(0.0, 4.0) = 1.0;

group_uniforms Flutter;
// Winziges Zittern einzelner Vertices, nur waehrend der Boe. Bewusst klein --
// hier entsteht sonst wieder das Wabern.
uniform float flutter_strength : hint_range(0.0, 0.1) = 0.012;
uniform float flutter_speed : hint_range(0.0, 8.0) = 3.0;

void vertex() {
    vec2 dir = normalize(wind_direction);
    vec3 wind_axis = vec3(dir.x, 0.0, dir.y);

    vec3 origin = MODEL_MATRIX[3].xyz;                       // Clustermitte in Weltkoordinaten
    vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec3 offset_from_origin = world_pos - origin;

    // Phase pro Cluster: entlang der Windrichtung wandernde Welle.
    float phase = dot(origin.xz, dir) * TAU / wind_wavelength + origin.y * 0.15;
    float t = TIME * wind_speed;

    // Grundschwingung plus eine schnellere Oberwelle, moduliert von der Boe.
    float sway = sin(t - phase) + 0.3 * sin(t * 2.3 - phase * 1.7);
    float gust = 1.0 - gust_amount
        + gust_amount * (0.5 + 0.5 * sin(TIME * gust_speed * TAU - phase * 0.35));

    float bend = sway * gust * wind_strength * sway_scale;

    // Hebelarm: Abstand zum Drehpunkt. Konstante Phase ueber den ganzen Cluster,
    // nur die Amplitude waechst nach oben -> koharentes Neigen statt Walken.
    float lever = max(offset_from_origin.y + pivot_offset, 0.0);

    vec3 world_offset = wind_axis * bend * lever;
    // Kleinwinkel-Naeherung der Kreisbahn: die Spitze senkt sich beim Neigen ab.
    world_offset.y -= 0.5 * bend * bend * lever;

    // Zittern auf Vertexebene, an die Boe gekoppelt.
    float flutter = sin(TIME * flutter_speed + dot(world_pos, vec3(2.1, 3.7, 1.9)));
    world_offset += wind_axis * flutter * flutter_strength * gust;

    // Weltversatz zurueck in den Modellraum des Clusters.
    VERTEX += (inverse(MODEL_MATRIX) * vec4(world_offset, 0.0)).xyz;
}

void fragment() {
    ALBEDO = leaf_color.rgb;
    ROUGHNESS = 0.85;
    SPECULAR = 0.15;
}
