#version 460 core

// Adapted from "3D Engine Design for Virtual Globes", Listing 4.7
// (diffuse-lighting vertex shader).
//
// The book writes `worldPosition = position.xyz` directly, which is only
// correct when the model matrix is the identity -- as in the book's
// SubdivisionSphere1 sample, where the mesh doesn't animate. This scene
// rotates the model every frame, so `position` is in MODEL space. We
// multiply by geode_modelMatrix to get the actual WORLD-space position
// before doing the lighting math; otherwise the lit hemisphere stays glued
// to fixed vertices and appears to rotate with the mesh.

in vec3 position;
out vec3 worldPosition;
out vec3 positionToLight;

uniform mat4 geode_modelViewPerspectiveMatrix;
uniform mat4 geode_modelMatrix;
uniform vec3 geode_cameraLightPosition;

void main()
{
    gl_Position = geode_modelViewPerspectiveMatrix * vec4(position, 1.0);
    worldPosition = (geode_modelMatrix * vec4(position, 1.0)).xyz;
    positionToLight = geode_cameraLightPosition - worldPosition;
}
