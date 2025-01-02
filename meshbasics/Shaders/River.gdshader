shader_type spatial;
#include "Water.gdshaderinc"
#include "HexCellData.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;
varying float visibility;
varying vec4 indices;

void vertex() {
	indices = CUSTOM0;
	vec4 cell0 = GetCellData(indices.x);
	vec4 cell1 = GetCellData(indices.y);

	visibility = cell0.x * COLOR.x  + cell1.x * COLOR.y;
	visibility = mix(0.25, 1.0, visibility);
}
void fragment() {
	float river = River(UV, _MainTex);
    vec4 c =  clamp(_Color + river, 0.0, 1.0);
    ALBEDO = c.rgb * visibility;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}