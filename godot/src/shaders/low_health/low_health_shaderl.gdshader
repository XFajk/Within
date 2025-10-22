shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE: hint_screen_texture, filter_linear_mipmap;

uniform sampler2D noise_texture;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float smooth_amount: hint_range(0.0, 1.0) = 0.3;

void fragment() {
	vec4 screen = texture(SCREEN_TEXTURE, SCREEN_UV);

	vec2 uv = SCREEN_UV;
	
	// Adjust UV coordinates for aspect ratio
	vec2 aspect_ratio = vec2(1.0, SCREEN_PIXEL_SIZE.x/SCREEN_PIXEL_SIZE.y);
	vec2 adjusted_uv = (uv - 0.5) * aspect_ratio + 0.5;

	float noise_dissolve = smoothstep(progress-smooth_amount, progress, (1.0-length(vec2(0.5)-adjusted_uv.xy)*(1.0 - texture(noise_texture, uv).r)));

	COLOR = vec4(screen.xyz * noise_dissolve, 1.0);
}
