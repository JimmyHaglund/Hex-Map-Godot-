shader_type spatial;

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;

void fragment() {
    vec2 uv1 = UV;
    uv1.x = uv1.x * 0.0625 + TIME * 0.005;
    uv1.y -= TIME * 0.25;
    vec4 noise1 = texture(_MainTex, uv1);
    
    vec2 uv2 = UV;
    uv2.x = uv2.x * 0.0625 - TIME * 0.0052;
    uv2.y -= TIME * 0.23;
    vec4 noise2 = texture(_MainTex, uv2);
    
    vec4 c =  _Color;
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}