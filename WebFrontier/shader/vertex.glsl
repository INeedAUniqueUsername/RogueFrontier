#version 300 es

//vertex var
layout(location = 0) in highp vec2 in_xy;
layout(location = 1) in highp vec2 in_tex;

//instance var
layout(location = 2) in highp vec2 in_xy_offset;
layout(location = 3) in highp vec2 in_tex_offset;
layout(location = 4) in highp vec4 in_rgba;

out highp vec4 _rgba;
out highp vec2 _tex;

// GLSL uses the reverse order to a System.Numerics.Matrix3x2
uniform mat2x3 viewprojection;

void main() {
	gl_Position = vec4(vec3(in_xy + in_xy_offset, 1.0) * viewprojection, 0.0, 1.0);
	_rgba = in_rgba;
	_tex = in_tex + in_tex_offset;
}