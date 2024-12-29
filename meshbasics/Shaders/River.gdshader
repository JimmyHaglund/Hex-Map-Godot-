shader_type spatial;
#include "Water.gdshaderinc"

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;

void fragment() {
	float river = River(UV, _MainTex);
    vec4 c =  clamp(_Color + river, 0.0, 1.0);
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}