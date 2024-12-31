shader_type spatial;
render_mode blend_mix, cull_back, depth_draw_opaque;

uniform bool GRID_ON;
uniform sampler2DArray _MainTex;
uniform sampler2D _GridTex;
uniform vec4 color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform int layer = 0;
varying vec3 terrain;

void vertex() {
	terrain = CUSTOM0.xyz;
}
vec4 GetTerrainColor (float t, vec3 worldPos, float clr) {
	vec3 uvw = vec3(worldPos.xz * 0.02, t);
	vec4 c = texture(_MainTex, uvw);
	return c * clr;
}
void fragment() {
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec4 c = (GetTerrainColor(terrain.x, world_vertex, COLOR.r)
	 + GetTerrainColor(terrain.y, world_vertex, COLOR.g)
	 + GetTerrainColor(terrain.z, world_vertex, COLOR.b));
	
	vec4 grid = vec4(1.0, 1.0, 1.0, 1.0);
	if (GRID_ON) {
		vec2 gridUV = world_vertex.xz;
		gridUV.x *= 1.0 / (4.0 * 8.66025404);
		gridUV.y *= 1.0 / (2.0 * 15.0);
		grid = texture(_GridTex, gridUV);
	}
	
	ALBEDO = c.rgb * grid.rgb * color.rgb;
    METALLIC = metallic;
    ROUGHNESS = 1.0 - glossiness;
}
