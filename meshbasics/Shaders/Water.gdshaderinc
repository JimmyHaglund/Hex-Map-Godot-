float Foam(float shore, vec2 worldXZ, sampler2D noiseTex) {
	shore = sqrt(shore) * 0.9;
	
	vec2 noiseUV = worldXZ + TIME * 0.25;
	vec4 noise = texture(noiseTex, noiseUV * 0.015);
	
	float distortion1 = noise.x * (1.0 - shore);
	float foam1 = sin((shore + distortion1) * 10.0 - TIME);
	foam1 *= foam1;
	
	float distortion2 = noise.y * (1.0 - shore);
	float foam2 = sin((shore + distortion2) * 10.0 + TIME + 2.0);
	foam2 *= foam2 * 0.7;
	
	return max(foam1, foam2) * shore;
}

float Waves(vec2 worldXZ, sampler2D noiseTex) {
	vec2 uv1 = worldXZ;
	uv1.y += TIME;
	vec4 noise1 = texture(noiseTex, uv1 * 0.025);
	
	vec2 uv2 = worldXZ;
	uv2.x += TIME;
	vec4 noise2 = texture(noiseTex, uv2 * 0.025);

	float blendWave = sin(
		(worldXZ.x + worldXZ.y) * 0.1 
		+ (noise1.y + noise2.z) + TIME
	);
	blendWave *= blendWave;
	
	float waves = mix(noise1.z, noise1.w, blendWave)
	 + mix(noise2.x, noise2.y, blendWave);
	return clamp(waves, 0.75, 2.0) - 1.0;
}