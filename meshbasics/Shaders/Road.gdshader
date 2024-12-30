shader_type spatial;

uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform sampler2D _MainTex;
uniform float _Glossiness;
uniform float _Metallic;
render_mode depth_draw_opaque;

void fragment() {
    vec2 modified_uv = UV;
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec4 noise = texture(_MainTex, world_vertex.xz * 0.025);
	
	float blend = modified_uv.x;
	blend *= noise.x + 0.5;
	blend = smoothstep(0.4, 0.7, blend);
	
	
    vec4 c = _Color * (noise.y * 0.75 + 0.25);
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = blend;
}
