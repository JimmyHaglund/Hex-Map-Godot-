shader_type spatial;

uniform sampler2D _MainTex;
uniform vec4 _Color : source_color = vec4(1, 1, 1, 1);
uniform float _Metallic;
uniform float _Glossiness;


void fragment() {
	float shore = UV.y;
	shore = sqrt(shore);
	
	vec3 world_vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec2 noiseUV = world_vertex.xz + TIME * 0.25;
	vec4 noise = texture(_MainTex, noiseUV * 0.015);
	
	float distortion1 = noise.x * (1.0 - shore);
	float foam1 = sin((shore + distortion1) * 10.0 - TIME);
	foam1 *= foam1;
	
	float distortion2 = noise.y * (1.0 - shore);
	float foam2 = sin((shore + distortion2) * 10.0 + TIME + 2.0);
	foam2 *= foam2 * 0.7;
	
	float foam = max(foam1, foam2) * shore;
	vec4 c = clamp(_Color + foam, 0.0, 1.0);
	
    ALBEDO = c.rgb;
    METALLIC = _Metallic;
    ROUGHNESS = 1.0 - _Glossiness;
    ALPHA = c.a;
}