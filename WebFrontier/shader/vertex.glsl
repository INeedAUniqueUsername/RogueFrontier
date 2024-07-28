#version 300 es

//instance var

layout(location = 0) in highp vec2 in_xy_pos;
layout(location = 1) in highp vec2 in_tex_pos;
layout(location = 2) in highp vec2 in_xy_size;
layout(location = 3) in highp vec2 in_tex_size;
layout(location = 4) in highp vec4 in_rgba;
//nw, ne, sw, se
const vec2 xy_points[] = vec2[] (
    vec2(0,0), vec2(1,0), vec2(0,1), vec2(1,1)
);
const vec2 tex_points[] = vec2[] (
    vec2(0,1), vec2(1,1), vec2(0,0), vec2(1,0)
);

out highp vec4 _rgba;
out highp vec2 _tex;

// GLSL uses the reverse order to a System.Numerics.Matrix3x2
uniform mat2x3 viewprojection;

void main() {
	gl_Position = vec4(vec3(in_xy_pos + 2.0 * in_xy_size * xy_points[gl_VertexID], 1.0) * viewprojection, 0.0, 1.0);
	_rgba = in_rgba;
	_tex = in_tex_pos + in_tex_size * tex_points[gl_VertexID];
}