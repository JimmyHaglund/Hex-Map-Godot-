uniform sampler2D hex_cell_data;
uniform vec4 texel_size;
uniform bool HEX_MAP_EDIT_MODE;

vec4 FilterCellData(vec4 data) {
	if (HEX_MAP_EDIT_MODE) {
		data.xy = vec2(1.0);
	}
	return data;
}

vec4 GetCellData(float cellIndex) {
	vec2 uv;
	uv.x = (cellIndex + 0.5) * texel_size.x;
	float row = floor(uv.x);
	uv.x -= row;
	uv.y = (row + 0.5) * texel_size.y;
	vec4 data = texture(hex_cell_data, uv);
	data.w *= 4.0; // Terrain type is stored here. We multiply by 4 to get an integer index.
	return FilterCellData(data);
}

vec4 GetCellData2(vec2 cellDataCoordinates) {
	vec2 uv = cellDataCoordinates + 0.5;
	uv.x *= texel_size.x;
	uv.y *= texel_size.y;
	vec4 result = texture(hex_cell_data, uv);
	return FilterCellData(result);
}

