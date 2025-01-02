shader_type spatial;
#include "Water.gdshaderinc"
#include "HexCellData.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;

varying vec4 indices;
varying float visibility;

void vertex() {
	indices = CUSTOM0;
	vec4 cell0 = GetCellData(indices.x);
	vec4 cell1 = GetCellData(indices.y);

	visibility = cell0.x * COLOR.x + cell1.x * COLOR.y;
	visibility = mix(0.25, 1.0, visibility);
}

void fragment() {
	float shore = UV.y;
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float foam = Foam(shore, world_vertex.xz, _MainTex);
	float waves = Waves(world_vertex.xz, _MainTex);
	waves *= 1.0 - shore;
	float shoreWater = max(foam, waves);
	float river = River(UV2, _MainTex);
	float water = mix(shoreWater, river, UV.x);
	vec4 c = clamp(_Color + water, 0.0, 1.0);
    ALBEDO = c.rgb * visibility;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}