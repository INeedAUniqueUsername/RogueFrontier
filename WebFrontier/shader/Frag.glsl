#version 300 es

in highp vec4 in_rgba;
in highp vec2 in_tex;

layout(location = 0) out highp vec4 diffuse;

void main() {
	diffuse = in_rgba;
}
