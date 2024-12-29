shader_type spatial;

uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform sampler2D _MainTex;
uniform float _Glossiness;
uniform float _Metallic;
render_mode depth_draw_opaque;

void fragment() {
    vec2 modified_uv = UV;
    vec4 c = _Color;
	
	float blend = modified_uv.x;
	blend = smoothstep(0.4, 0.7, blend);
	
	
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = blend;
	DEPTH = 1.0;
}
