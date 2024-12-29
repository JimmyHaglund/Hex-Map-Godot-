shader_type spatial;

uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform sampler2D _MainTex;
uniform float _Glossiness;
uniform float _Metallic;
render_mode depth_draw_opaque;

void fragment() {
    vec2 modified_uv = UV;
    vec4 c = vec4(modified_uv, 1.0, 1.0);

    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
	DEPTH = 1.0;
}
