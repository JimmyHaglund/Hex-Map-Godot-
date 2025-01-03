shader_type spatial;
render_mode depth_draw_always, cull_back;
#include "HexCellData.gdshaderinc"

uniform vec4 u_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D u_main_tex;
uniform sampler2D u_grid_coordinates : filter_nearest;
uniform float u_glossiness : hint_range(0.0, 1.0) = 0.5;
// uniform float u_metallic : hint_range(0.0, 1.0) = 0.0;
uniform float _Specular : hint_range(0.0, 1.0) = 0.2;
uniform vec3 _BackgroundColor : source_color = vec3(0.0, 0.0, 0.0);

// Input to simulate Unity's Input struct
varying vec2 uv_main_tex;
varying vec2 visibility;

void vertex() {
    vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec4 gridUV = vec4(world_pos.xz, world_pos.xz);
	gridUV.x *= 1.0 / (4.0 * 8.66025404);
	gridUV.y *= 1.0 / (2.0 * 15.0);
	
	vec2 offset = texture(u_grid_coordinates, gridUV.xy).rg;
	vec2 worldCoordinates = floor(gridUV.xy);
	vec2 cellDataCoordinates = floor(gridUV.xy) + texture(u_grid_coordinates, gridUV.xy).rg;
	cellDataCoordinates *= 2.0;
	
	vec4 cellData = GetCellData2(cellDataCoordinates);
	visibility.x = cellData.x;
	visibility.x = mix(0.25, 1.0, visibility.x);
	visibility.y = cellData.y;
}

void fragment() {
	float explored = visibility.y;
    vec4 color = texture(u_main_tex, UV) * u_color;
    ALBEDO = color.rgb * visibility.x * explored;
    SPECULAR = _Specular * explored;
    ROUGHNESS = 1.0 - u_glossiness;
	EMISSION = _BackgroundColor * (1.0 - explored);
    ALPHA = color.a;
}
