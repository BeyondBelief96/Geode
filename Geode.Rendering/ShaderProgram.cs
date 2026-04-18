// Geode.Rendering/ShaderProgram.cs
//
// Compiles a vertex + fragment shader pair and links them into a program.
// Provides helpers for setting common uniform types.
//
// Uses non-DSA shader compilation (glCreateShader / glCompileShader)
// and non-DSA linking (glCreateProgram / glLinkProgram) because the
// DSA spec does not cover shader objects.

using Silk.NET.OpenGL;
using System;

namespace Geode.Rendering
{
    /// <summary>
    /// A compiled and linked GPU shader program (vertex + fragment stages).
    /// After construction the intermediate shader objects are detached and deleted.
    /// </summary>
    public class ShaderProgram : IDisposable
    {
        private readonly GL _gl;
        private readonly uint _handle;

        /// <summary>The raw OpenGL program handle.</summary>
        public uint Handle => _handle;

        /// <summary>
        /// Compiles and links a shader program from GLSL source strings.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertexSource">GLSL source for the vertex stage.</param>
        /// <param name="fragmentSource">GLSL source for the fragment stage.</param>
        /// <exception cref="Exception">
        /// Thrown when either shader fails to compile or when linking fails.
        /// The exception message contains the driver info log.
        /// </exception>
        public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            // Compile both stages independently
            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            // Create the program and attach both compiled stages
            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);
            _gl.AttachShader(_handle, fragmentShader);

            // Link the stages into a complete pipeline
            _gl.LinkProgram(_handle);

            // Check for link errors
            _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int status);
            if(status == 0)
            {
                throw new Exception($"Shader link error: {_gl.GetProgramInfoLog(_handle)}");
            }

            // Detach and delete the individual shader objects — the linked
            // program retains its own copy of the compiled code.
            _gl.DetachShader(_handle, vertexShader);
            _gl.DetachShader(_handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }

        /// <summary>
        /// Convenience factory that reads vertex and fragment sources from disk.
        /// </summary>
        /// <param name="gl">The Silk.NET OpenGL context.</param>
        /// <param name="vertexPath">File path to the vertex shader source.</param>
        /// <param name="fragmentPath">File path to the fragment shader source.</param>
        /// <returns>A compiled and linked <see cref="ShaderProgram"/>.</returns>
        public static ShaderProgram FromFiles(GL gl, string vertexPath, string fragmentPath)
        {
            string vertexSource = System.IO.File.ReadAllText(vertexPath);
            string fragmentSource = System.IO.File.ReadAllText(fragmentPath);
            return new ShaderProgram(gl, vertexSource, fragmentSource);
        }

        /// <summary>Binds this program as the active shader pipeline.</summary>
        public void Use() => _gl.UseProgram(_handle);

        /// <summary>Sets an <c>int</c> (or <c>sampler2D</c>) uniform.</summary>
        /// <param name="name">The uniform name as declared in GLSL.</param>
        /// <param name="value">The value to set.</param>
        public void SetInt(string name, int value) => _gl.Uniform1(GetUniformLocation(name), value);

        /// <summary>Sets a <c>float</c> uniform.</summary>
        /// <param name="name">The uniform name as declared in GLSL.</param>
        /// <param name="value">The value to set.</param>
        public void SetFloat(string name, float value) => _gl.Uniform1(GetUniformLocation(name), value);

        /// <summary>Sets a <c>vec3</c> uniform.</summary>
        /// <param name="name">The uniform name as declared in GLSL.</param>
        public void SetVec3(string name, float x, float y, float z) => _gl.Uniform3(GetUniformLocation(name), x, y, z);

        /// <summary>Sets a <c>vec4</c> uniform.</summary>
        /// <param name="name">The uniform name as declared in GLSL.</param>
        public void SetVec4(string name, float x, float y, float z, float w) => _gl.Uniform4(GetUniformLocation(name), x, y, z, w);

        /// <summary>Sets a <c>mat4</c> uniform from a 16-element column-major float array.</summary>
        /// <param name="name">The uniform name as declared in GLSL.</param>
        /// <param name="mat">A 16-element array in column-major order.</param>
        public void SetMat4(string name, float[] mat) => _gl.UniformMatrix4(GetUniformLocation(name), 1, false, mat);

        /// <summary>
        /// Looks up a uniform location by name. Returns -1 for uniforms that do not
        /// exist or were optimized out by the driver — this is not treated as an error.
        /// </summary>
        private int GetUniformLocation(string name)
        {
            int loc = _gl.GetUniformLocation(_handle, name);
            return loc;
        }

        /// <summary>
        /// Compiles a single shader stage from source and returns its GL handle.
        /// </summary>
        /// <exception cref="Exception">Thrown when compilation fails.</exception>
        private uint CompileShader(ShaderType type, string source)
        {
            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            // Check for compilation errors and surface the driver log
            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                string log = _gl.GetShaderInfoLog(shader);
                _gl.DeleteShader(shader);
                throw new Exception($"Failed to compile {type} shader: {log}");
            }

            return shader;
        }

        /// <summary>Deletes the GPU program object.</summary>
        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}
