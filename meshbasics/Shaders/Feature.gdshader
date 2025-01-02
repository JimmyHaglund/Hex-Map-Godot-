shader_type spatial;
render_mode blend_mix, cull_back, depth_draw_opaque;
#include "HexCellData.gdshaderinc"

uniform vec4 u_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D u_main_tex;
uniform sampler2D u_grid_coordinates;
uniform float u_glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float u_metallic : hint_range(0.0, 1.0) = 0.0;

// Input to simulate Unity's Input struct
varying vec2 uv_main_tex;
varying float visibility;

void vertex() {
    vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
    uv_main_tex = UV;
    visibility = 1.0;
}

void fragment() {
    vec4 color = texture(u_main_tex, uv_main_tex) * u_color;

    ALBEDO = color.rgb * visibility;
    METALLIC = u_metallic;
    ROUGHNESS = 1.0 - u_glossiness;
    ALPHA = color.a;
}
