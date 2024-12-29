shader_type spatial;

uniform sampler2D _MainTex;
uniform vec4 _Color;
uniform float _Metallic;
uniform float _Glossiness;

void fragment() {
    vec4 c = texture(_MainTex, UV) * _Color;

    ALBEDO = c.rgb * COLOR.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
    vec2 modified_uv = UV;
    if (modified_uv.y < 0.0) {
        modified_uv.y = 1.0;
    }
	
    ALBEDO.rg = modified_uv;
}