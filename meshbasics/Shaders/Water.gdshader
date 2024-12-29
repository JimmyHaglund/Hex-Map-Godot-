shader_type spatial;

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;


void fragment() {
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec2 uv1 = world_vertex.xz;
	uv1.y += TIME;
	vec4 noise1 = texture(_MainTex, uv1 * 0.025);
	
	vec2 uv2 = world_vertex.xz;
	uv2.x += TIME;
	vec4 noise2 = texture(_MainTex, uv2 * 0.025);
	
	float waves = noise1.z + noise2.x;
	waves = clamp(waves, 0.75, 2.0) - 1.0;
    vec4 c = clamp(_Color + waves, 0.0, 0.9);
	
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}