shader_type spatial;
render_mode blend_mix, cull_back, depth_draw_opaque;

uniform sampler2D main_texture;
uniform vec4 color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float glossiness : hint_range(0.0, 1.0) = 0.5;
uniform float metallic : hint_range(0.0, 1.0) = 0.0;

void fragment() {
    // Fetch the texture color
    vec4 tex_color = texture(main_texture, UV);

    // Multiply the texture color by the uniform color and vertex color
    vec4 final_color = tex_color * color * COLOR;

    // Assign the albedo and metallic/smoothness properties
    ALBEDO = final_color.rgb;
    METALLIC = metallic;
    ROUGHNESS = 1.0 - glossiness; // Godot uses roughness instead of smoothness
}
