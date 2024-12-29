shader_type spatial;

uniform sampler2D _MainTex;
uniform vec4 _Color;
uniform float _Metallic;
uniform float _Glossiness;

void fragment() {
    vec2 modified_uv = UV;
    modified_uv.x *= 0.0625;
    modified_uv.y -= TIME * 0.25;

    vec4 noise = texture(_MainTex, modified_uv);

    vec4 c = _Color * noise.r;
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}