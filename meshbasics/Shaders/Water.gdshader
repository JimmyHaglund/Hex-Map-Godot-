shader_type spatial;
#include "Water.gdshaderinc"
#include "HexCellData.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color;
uniform float _Metallic;
uniform float _Glossiness;
varying vec4 indices;
varying float visibility;

void vertex() {
	indices = CUSTOM0;
	vec4 cell0 = GetCellData(indices.x);
	vec4 cell1 = GetCellData(indices.y);
	vec4 cell2 = GetCellData(indices.z);

	visibility = cell0.x * COLOR.x + cell1.x * COLOR.y + cell2.x * COLOR.z;
	visibility = mix(0.05, 1.0, visibility);
}

void fragment() {
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float waves = Waves(world_vertex.xz, _MainTex);
	vec4 c = clamp(_Color + waves, 0.0, 1.0);

    ALBEDO = c.rgb * visibility;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}