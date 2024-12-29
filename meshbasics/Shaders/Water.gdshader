shader_type spatial;
#include "Water.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;


void fragment() {
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	float waves = Waves(world_vertex.xz, _MainTex);
	vec4 c = clamp(_Color + waves, 0.0, 1.0);
	
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}