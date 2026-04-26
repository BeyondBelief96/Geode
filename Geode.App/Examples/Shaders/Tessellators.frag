#version 460 core

in vec3 worldPosition;
in vec3 positionToLight;

layout(location = 0) out vec4 fragmentColor;

void main()
{
    vec3 toLight = normalize(positionToLight);
    vec3 normal = normalize(worldPosition);
    float diffuse = max(dot(toLight, normal), 0.0);
    fragmentColor = vec4(diffuse, diffuse, diffuse, 1.0);
}
