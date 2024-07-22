#version 300 es

in highp vec4 in_rgba;
in highp vec2 in_tex;

uniform sampler2D uSampler;

layout(location = 0) out highp vec4 diffuse;

void main() {	
	diffuse = in_rgba;
	//diffuse = texture(uSampler, in_tex)
}
