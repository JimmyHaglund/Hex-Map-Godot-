global uniform sampler2D HEX_CELL_DATA;
global uniform vec4 HEX_TEXEL_SIZE;
global uniform bool HEX_MAP_EDIT_MODE;

vec4 FilterCellData(vec4 data) {
	if (HEX_MAP_EDIT_MODE) {
		data.xy = vec2(1.0);
	}
	return data;
}

vec4 GetCellData(float cellIndex) {
	vec2 uv;
	uv.x = (cellIndex + 0.5) * HEX_TEXEL_SIZE.x;
	float row = floor(uv.x);
	uv.x -= row;
	uv.y = (row + 0.5) * HEX_TEXEL_SIZE.y;
	vec4 data = texture(HEX_CELL_DATA, uv);
	data.w *= 4.0; // Terrain type is stored here. We multiply by 4 to get an integer index.
	return FilterCellData(data);
}

vec4 GetCellData2(vec2 cellDataCoordinates) {
	vec2 uv = cellDataCoordinates + 0.5;
	uv.x *= HEX_TEXEL_SIZE.x;
	uv.y *= HEX_TEXEL_SIZE.y;
	vec4 result = texture(HEX_CELL_DATA, uv);
	return FilterCellData(result);
}

