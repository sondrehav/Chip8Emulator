#version 330 core

layout(location = 0)in vec2 vertex;

float bulge = .2;
float pi = 3.14159265359;

vec2 transform(vec2 inputVector)
{
	float x = bulge * sin(pi * inputVector.x / 2.0) + (inputVector.x * (1.0 - bulge));
	float y = bulge * sin(pi * inputVector.y / 2.0) + (inputVector.y * (1.0 - bulge));
	return vec2(x, y);
}

void main()
{
	gl_Position = vec4(transform(vertex), 0.0, 1.0);
}