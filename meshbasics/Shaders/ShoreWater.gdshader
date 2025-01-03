shader_type spatial;
#include "Water.gdshaderinc"
#include "HexCellData.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Specular : hint_range(0.0, 1.0) = 0.2;
uniform float _Glossiness;
varying vec4 indices;
varying vec2 visibility;

void vertex() {
	indices = CUSTOM0;
	vec4 cell0 = GetCellData(indices.x);
	vec4 cell1 = GetCellData(indices.y);
	vec4 cell2 = GetCellData(indices.z);

	visibility.x = cell0.x * COLOR.x + cell1.x * COLOR.y + cell2.x * COLOR.z;
	visibility.x = mix(0.25, 1.0, visibility.x);
	visibility.y = cell0.y * COLOR.x + cell1.y * COLOR.y + cell2.y * COLOR.z;
}

void fragment() {
	float shore = UV.y;
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float foam = Foam(shore, world_vertex.xz, _MainTex);
	float waves = Waves(world_vertex.xz, _MainTex);
	waves *= 1.0 - shore;
	vec4 c = clamp(_Color + max(foam, waves), 0.0, 1.0);
	float explored = visibility.y;
	
    ALBEDO = c.rgb * visibility.x;
    SPECULAR = _Specular * explored;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a * explored;
}