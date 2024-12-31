shader_type spatial;
render_mode blend_mix, cull_back, depth_draw_opaque;

uniform sampler2DArray _MainTex;
uniform vec4 color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform int layer = 0;

void fragment() {
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec2 uv = world_vertex.xz * 0.02;
	
    vec4 c = texture(_MainTex, vec3(uv, float(layer)));
    ALBEDO = c.rgb;
    METALLIC = metallic;
    ROUGHNESS = 1.0 - glossiness;
}
