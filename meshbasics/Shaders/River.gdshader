shader_type spatial;
#include "Water.gdshaderinc"
#include "HexCellData.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float _Specular : hint_range(0.0, 1.0) = 0.2;
uniform float _Glossiness;
varying vec4 indices;
varying vec2 visibility;

void vertex() {
	indices = CUSTOM0;
	vec4 cell0 = GetCellData(indices.x);
	vec4 cell1 = GetCellData(indices.y);

	visibility.x = cell0.x * COLOR.x  + cell1.x * COLOR.y;
	visibility.x = mix(0.25, 1.0, visibility.x);
	visibility.y = cell0.y * COLOR.x + cell1.y * COLOR.y;
}
void fragment() {
	float river = River(UV, _MainTex);
    vec4 c =  clamp(_Color + river, 0.0, 1.0);
	float explored = visibility.y;
    ALBEDO = c.rgb * visibility.x;
    SPECULAR = _Specular * explored;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a * explored;
}