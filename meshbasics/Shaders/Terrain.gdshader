shader_type spatial;
#include "HexCellData.gdshaderinc"

render_mode blend_mix, cull_back, depth_draw_opaque;

uniform bool GRID_ON;
uniform sampler2DArray _MainTex;
uniform sampler2D _GridTex;
uniform vec4 color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float glossiness : hint_range(0.0, 1.0) = 0.5;
// uniform float metallic : hint_range(0.0, 1.0) = 0.0;
uniform float _Specular : hint_range(0.0, 1.0) = 0.2;
uniform int layer = 0;
varying vec4 indices;
varying vec4 visibility;

void vertex() {
	indices = CUSTOM0;
	
 	vec4 cell0 = GetCellData(indices.x);
 	vec4 cell1 = GetCellData(indices.y);
 	vec4 cell2 = GetCellData(indices.z);
 	
	indices.x = cell0.w;
 	indices.y = cell1.w;
 	indices.z = cell2.w;
	
	visibility.x = cell0.x;
	visibility.y = cell1.x;
	visibility.z = cell2.x;
	visibility.xyz = mix(vec3(0.25), vec3(1.0), visibility.xyz);
	visibility.w = cell0.y * COLOR.x + cell1.y * COLOR.y + cell2.y * COLOR.z;
}
vec4 GetTerrainColor (vec3 worldPos, vec4 clr, int index) {
	vec3 uvw = vec3(worldPos.xz * 0.02, indices[index]);
	vec4 c = texture(_MainTex, uvw);
	return c * (clr[index] * visibility[index]);
}

void fragment() {
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    vec4 c = (GetTerrainColor(world_vertex, COLOR, 0)
	 + GetTerrainColor(world_vertex, COLOR, 1)
	 + GetTerrainColor(world_vertex, COLOR, 2));
	
	// vec3 specular = _Specular.xyz;
	// vec3 view_dir = normalize(-VIEW);
    // vec3 light_dir = normalize(light());
    // vec3 half_vec = normalize(view_dir + light_dir);
	
	vec4 grid = vec4(1.0, 1.0, 1.0, 1.0);
	if (GRID_ON) {
		vec2 gridUV = world_vertex.xz;
		gridUV.x *= 1.0 / (4.0 * 8.66025404);
		gridUV.y *= 1.0 / (2.0 * 15.0);
		grid = texture(_GridTex, gridUV);
	}
	float explored = visibility.w;
	ALBEDO = c.rgb * grid.rgb * color.rgb * explored;
	SPECULAR = _Specular * explored;
    ROUGHNESS = 1.0 - glossiness;
}
