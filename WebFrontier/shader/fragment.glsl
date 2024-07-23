#version 300 es

in highp vec4 _rgba;
in highp vec2 _tex;

layout(location = 0) out highp vec4 diffuse;

uniform sampler2D uSampler;

void main() {	
	//diffuse = _rgba;
	diffuse = texture(uSampler, _tex) * _rgba;
}
