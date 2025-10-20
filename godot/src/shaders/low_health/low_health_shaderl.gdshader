shader_type canvas_item;

uniform sampler2D SCREEN_TEXTURE: hint_screen_texture, filter_linear_mipmap;

uniform sampler2D noise_texture;
uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float smooth_amount: hint_range(0.0, 1.0) = 0.3;

void fragment() {
	vec4 screen = texture(SCREEN_TEXTURE, SCREEN_UV);
	
	float noise_dissolve = smoothstep(progress-smooth_amount, progress, (1.0-length(vec2(0.5)-UV.xy)*(1.0 - texture(noise_texture, SCREEN_UV).r)));
	
	COLOR = vec4(screen.xyz * noise_dissolve, 1.0);
}
