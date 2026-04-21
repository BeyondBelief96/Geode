// ShaderProgram -- compiled and linked GLSL program with a typed uniform collection
// and an automatic-uniform pipeline.
//
// Book Chapter 3, Sections 3.4.1 (compile/link), 3.4.3 (fragment outputs),
// 3.4.4 (uniforms), 3.4.5 (automatic uniforms).
// OpenGlobe: Source/Renderer/GL3x/Shaders/ShaderProgramGL3x.cs
//
// Responsibilities (in order of what the constructor does):
//   1. Compile vertex + fragment GLSL source, link into a program object.
//       Throw on any compile or link error, surfacing the driver log.
//   2. Delete the intermediate shader objects once linking succeeds. The
//       program retains its own copy of the compiled code.
//   3. Populate the Uniforms collection by scanning active uniforms via
//       glGetActiveUniform and dispatching each to the right concrete
//       Uniform<T> subclass. Inactive uniforms (optimized out by the driver)
//       are skipped.
//   4. Wire up automatic uniforms: every uniform whose name is registered
//       in AutomaticUniformFactoryCollection becomes either a link-automatic
//       (fired once now) or a draw-automatic (stored for Bind() to invoke
//       before every draw).
//   5. Expose a FragmentOutputs collection for framebuffer setup.
//
// At draw time, RenderContext calls Bind(ctx, drawState, sceneState), which
// useProgram's this handle, runs every DrawAutomaticUniform (each writes
// into its captured Uniform<T>.Value, dirtying the uniform if the value
// changed), then Clean()s the dirty list -- one glProgramUniform* call per
// actually-changed uniform.
//
// Non-DSA GL functions (glCreateShader, glLinkProgram, glAttachShader) are
// used because the DSA specification does not cover shader objects.

using Geode.Rendering.Shaders.Uniforms;
using Geode.Rendering.Shaders.Uniforms.GL;
using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using UniformType = Geode.Rendering.Uniforms.UniformType;
using SilkUniformType = Silk.NET.OpenGL.UniformType;

namespace Geode.Rendering.Shaders
{
    /// <summary>
    /// A compiled and linked GLSL shader program. Owns a typed
    /// <see cref="UniformCollection"/>, a list of automatic-uniform setters populated at
    /// link time, and a dirty list of uniforms pending upload to the GPU.
    /// </summary>
    public class ShaderProgram : IDisposable, ICleanableObserver
    {
        private readonly GL _gl;
        private readonly uint _handle;

        private readonly UniformCollection _uniforms = new();
        private readonly List<ICleanable> _dirtyUniforms = new();
        private readonly List<DrawAutomaticUniform> _drawAutomaticUniforms = new();
        private readonly FragmentOutputs _fragmentOutputs;

        /// <summary>The raw OpenGL program handle.</summary>
        public uint Handle => _handle;

        /// <summary>
        /// Every active uniform this shader declares, keyed by GLSL name. Populated at
        /// link time. For per-draw manual uniforms use
        /// <c>((Uniform&lt;T&gt;)program.Uniforms["name"]).Value = ...</c>
        /// or the typed setter shortcuts (<see cref="SetInt"/>, <see cref="SetFloat"/>,
        /// <see cref="SetVec3(string, System.Numerics.Vector3)"/>,
        /// <see cref="SetVec4(string, System.Numerics.Vector4)"/>, <see cref="SetMat4"/>).
        /// </summary>
        public UniformCollection Uniforms => _uniforms;

        /// <summary>
        /// Name-to-color-attachment-index mapping for the fragment shader's <c>out</c>
        /// variables. Used by a framebuffer wrapper to route named outputs to
        /// specific attachment slots.
        /// </summary>
        public FragmentOutputs FragmentOutputs => _fragmentOutputs;

        // ---------------------------------------------------------------
        // Construction
        // ---------------------------------------------------------------

        /// <summary>
        /// Compiles and links a shader program from GLSL source strings, then
        /// populates <see cref="Uniforms"/> and the automatic-uniform pipeline.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when compilation or linking fails. The message contains the driver info log.
        /// </exception>
        public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
        {
            _gl = gl;

            uint vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, vertexShader);
            _gl.AttachShader(_handle, fragmentShader);
            _gl.LinkProgram(_handle);

            _gl.GetProgram(_handle, ProgramPropertyARB.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
            {
                string log = _gl.GetProgramInfoLog(_handle);
                _gl.DeleteProgram(_handle);
                _gl.DeleteShader(vertexShader);
                _gl.DeleteShader(fragmentShader);
                throw new Exception($"Shader link error: {log}");
            }

            // Detach + delete the intermediate shader objects. The linked program
            // retains its own copy of the compiled code.
            _gl.DetachShader(_handle, vertexShader);
            _gl.DetachShader(_handle, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);

            // Populate the typed uniform collection and wire up automatic uniforms.
            InitializeUniforms();
            InitializeAutomaticUniforms();

            // Fragment outputs are queried lazily -- the constructor just captures the handle.
            _fragmentOutputs = new FragmentOutputs(_gl, _handle);
        }

        /// <summary>Loads GLSL source files from disk and builds a program from them.</summary>
        public static ShaderProgram FromFiles(GL gl, string vertexPath, string fragmentPath)
        {
            return new ShaderProgram(gl,
                System.IO.File.ReadAllText(vertexPath),
                System.IO.File.ReadAllText(fragmentPath));
        }

        // ---------------------------------------------------------------
        // Per-draw entry point
        // ---------------------------------------------------------------

        /// <summary>
        /// Bind the program and flush all pending uniform uploads.
        /// Called by <see cref="RenderContext"/> immediately before a draw call:
        /// 1. Makes this program current via <c>glUseProgram</c>.
        /// 2. Runs every draw-automatic uniform, writing into the captured
        ///     <see cref="Uniform{T}"/> values. Each assignment marks its uniform
        ///     dirty only if the new value differs from the cached one.
        /// 3. Flushes the dirty list via <see cref="Clean"/> -- one
        ///     <c>glProgramUniform*</c> call per uniform whose value actually changed.
        /// </summary>
        public void Bind(RenderContext context, DrawState drawState, SceneState sceneState)
        {
            _gl.UseProgram(_handle);

            foreach (DrawAutomaticUniform auto in _drawAutomaticUniforms)
                auto.Set(context, drawState, sceneState);

            Clean();
        }

        /// <summary>
        /// Flush every dirty uniform to the GPU. Normally invoked by <see cref="Bind"/>;
        /// exposed publicly for tests and diagnostic code that wants to force an
        /// upload outside a draw.
        /// </summary>
        public void Clean()
        {
            foreach (ICleanable cleanable in _dirtyUniforms)
                cleanable.Clean();
            _dirtyUniforms.Clear();
        }

        /// <summary>
        /// Low-level: make this program the current GL program (<c>glUseProgram</c>).
        /// For draws, prefer <see cref="Bind"/>. <see cref="Use"/> exists for the rare
        /// case where client code wants the program current outside a draw (diagnostics,
        /// manual uniform introspection). Setting uniforms through
        /// <see cref="Uniforms"/> does not require the program to be current --
        /// <c>glProgramUniform*</c> works on any program handle.
        /// </summary>
        public void Use() => _gl.UseProgram(_handle);

        // ---------------------------------------------------------------
        // Typed convenience setters.
        // Shortcuts over ((Uniform<T>)Uniforms[name]).Value = value -- they
        // route through the typed collection and dirty list exactly the same
        // way. They do NOT bypass dirtying like the pre-rewrite helpers did.
        // ---------------------------------------------------------------

        /// <summary>
        /// Set an <c>int</c> uniform -- also used for sampler uniforms, whose value
        /// is the texture-unit index to sample from.
        /// </summary>
        public void SetInt(string name, int value)
            => ((Uniform<int>)_uniforms[name]).Value = value;

        /// <summary>Set a <c>float</c> uniform.</summary>
        public void SetFloat(string name, float value)
            => ((Uniform<float>)_uniforms[name]).Value = value;

        /// <summary>Set a <c>vec3</c> uniform from three scalars.</summary>
        public void SetVec3(string name, float x, float y, float z)
            => ((Uniform<Vector3>)_uniforms[name]).Value = new Vector3(x, y, z);

        /// <summary>Set a <c>vec3</c> uniform from a <see cref="Vector3"/>.</summary>
        public void SetVec3(string name, Vector3 value)
            => ((Uniform<Vector3>)_uniforms[name]).Value = value;

        /// <summary>Set a <c>vec4</c> uniform from four scalars.</summary>
        public void SetVec4(string name, float x, float y, float z, float w)
            => ((Uniform<Vector4>)_uniforms[name]).Value = new Vector4(x, y, z, w);

        /// <summary>Set a <c>vec4</c> uniform from a <see cref="Vector4"/>.</summary>
        public void SetVec4(string name, Vector4 value)
            => ((Uniform<Vector4>)_uniforms[name]).Value = value;

        /// <summary>Set a <c>mat4</c> uniform from a <see cref="Matrix4x4"/>.</summary>
        public void SetMat4(string name, Matrix4x4 value)
            => ((Uniform<Matrix4x4>)_uniforms[name]).Value = value;

        // ---------------------------------------------------------------
        // ICleanableObserver
        // ---------------------------------------------------------------

        // Uniform<T>.Value setters call this when their cached value changes.
        // We append to the dirty list so the next Clean() flushes exactly
        // the uniforms that actually changed.
        void ICleanableObserver.NotifyDirty(ICleanable cleanable) => _dirtyUniforms.Add(cleanable);

        // ---------------------------------------------------------------
        // Uniform discovery + dispatch (called once, at construction)
        // ---------------------------------------------------------------

        /// <summary>
        /// Scan every active uniform declared by the linked program and wrap it in
        /// the matching concrete <see cref="Uniform{T}"/> subclass. Inactive uniforms
        /// (declared but optimized away, location -1) are skipped.
        /// </summary>
        private void InitializeUniforms()
        {
            _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniforms, out int count);
            _gl.GetProgram(_handle, ProgramPropertyARB.ActiveUniformMaxLength, out int maxNameLength);

            for (uint i = 0; i < (uint)count; i++)
            {
                _gl.GetActiveUniform(_handle, i, (uint)maxNameLength,
                    out _, out _, out SilkUniformType silkType, out string name);

                // Array uniforms show up with a "[0]" suffix in the reflection data;
                // glGetUniformLocation expects the bare name.
                int bracket = name.IndexOf('[');
                if (bracket >= 0) name = name.Substring(0, bracket);

                int location = _gl.GetUniformLocation(_handle, name);
                if (location < 0) continue;  // optimized out by the driver -- skip silently

                UniformType type = (UniformType)silkType;
                Uniform uniform = CreateConcreteUniform(type, name, location);
                _uniforms.Add(uniform);
            }
        }

        /// <summary>
        /// Factory for concrete <see cref="Uniform{T}"/> subclasses, dispatching on the
        /// GLSL type reported by <c>glGetActiveUniform</c>.
        /// </summary>
        /// <exception cref="NotSupportedException">
        /// Thrown when a shader declares a uniform of a type we have not wired up yet
        /// (e.g. a non-square matrix variant, or a less common sampler). Add the
        /// corresponding concrete GL class and extend this switch.
        /// </exception>
        private Uniform CreateConcreteUniform(UniformType type, string name, int location)
        {
            return type switch
            {
                UniformType.Float         => new UniformFloatGL(_gl, _handle, name, location, this),
                UniformType.FloatVector2  => new UniformFloatVector2GL(_gl, _handle, name, location, this),
                UniformType.FloatVector3  => new UniformFloatVector3GL(_gl, _handle, name, location, this),
                UniformType.FloatVector4  => new UniformFloatVector4GL(_gl, _handle, name, location, this),
                UniformType.FloatMatrix33 => new UniformFloatMatrix33GL(_gl, _handle, name, location, this),
                UniformType.FloatMatrix44 => new UniformFloatMatrix44GL(_gl, _handle, name, location, this),

                // One class covers plain int plus every sampler variant. The concrete
                // Uniform's Datatype preserves the actual GLSL type so automatic-uniform
                // factories (e.g. TextureUniform on Sampler2D) can still type-check.
                UniformType.Int
                    or UniformType.Sampler1D
                    or UniformType.Sampler2D
                    or UniformType.Sampler3D
                    or UniformType.SamplerCube
                    or UniformType.Sampler2DArray
                    or UniformType.Sampler2DShadow
                        => new UniformIntGL(_gl, _handle, name, location, type, this),

                UniformType.Bool         => new UniformBoolGL(_gl, _handle, name, location, this),
                UniformType.IntVector2   => new UniformIntVector2GL(_gl, _handle, name, location, this),

                _ => throw new NotSupportedException(
                    $"Uniform '{name}' has GLSL type {type}, which is not yet wired up. " +
                    $"Add the corresponding concrete Uniform class in Shaders/Uniforms/GL/ " +
                    $"and extend ShaderProgram.CreateConcreteUniform.")
            };
        }

        /// <summary>
        /// For each uniform the shader declares, check the automatic-uniform registry.
        /// Link-automatics are fired once (here, now), writing a fixed value through
        /// the <see cref="Uniform{T}.Value"/> setter so it eventually flushes on the
        /// first <see cref="Clean"/>. Draw-automatics have a factory create a
        /// <see cref="DrawAutomaticUniform"/> setter that <see cref="Bind"/> invokes
        /// before every draw.
        /// </summary>
        /// <exception cref="Exception">
        /// Thrown when a shader declares an automatic uniform with a GLSL type that
        /// doesn't match what the registered factory expects -- catches typos like
        /// <c>uniform vec3 geode_modelViewPerspectiveMatrix;</c> at link time rather
        /// than surfacing as garbage values at draw time.
        /// </exception>
        private void InitializeAutomaticUniforms()
        {
            foreach (Uniform uniform in _uniforms)
            {
                // Link-automatic first -- set once now, never again.
                if (AutomaticUniformFactoryCollection.TryGetLink(uniform.Name, out LinkAutomaticUniform? linkAuto))
                {
                    if (linkAuto!.DataType != uniform.Datatype)
                    {
                        throw new Exception(
                            $"Shader declares '{uniform.Name}' as {uniform.Datatype}, " +
                            $"but the engine's link-automatic expects {linkAuto.DataType}.");
                    }
                    linkAuto.Set(uniform);
                    continue;
                }

                // Draw-automatic: build the setter and store it for Bind() to invoke.
                if (AutomaticUniformFactoryCollection.TryGetDrawFactory(uniform.Name, out DrawAutomaticUniformFactory? drawFactory))
                {
                    if (drawFactory!.DataType != uniform.Datatype)
                    {
                        throw new Exception(
                            $"Shader declares '{uniform.Name}' as {uniform.Datatype}, " +
                            $"but the engine's draw-automatic factory expects {drawFactory.DataType}.");
                    }
                    _drawAutomaticUniforms.Add(drawFactory.Create(uniform));
                }

                // Anything else is a manual uniform -- the application sets it explicitly
                // via the Uniforms collection or the typed SetXxx helpers.
            }
        }

        // ---------------------------------------------------------------
        // Shader compilation
        // ---------------------------------------------------------------

        private uint CompileShader(ShaderType type, string source)
        {
            uint shader = _gl.CreateShader(type);
            _gl.ShaderSource(shader, source);
            _gl.CompileShader(shader);

            _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                string log = _gl.GetShaderInfoLog(shader);
                _gl.DeleteShader(shader);
                throw new Exception($"Failed to compile {type} shader: {log}");
            }

            return shader;
        }

        // ---------------------------------------------------------------
        // Disposal
        // ---------------------------------------------------------------

        /// <summary>
        /// Deletes the GPU program object. Must run on the render thread.
        /// Automatic uniform setters hold references to our <see cref="Uniform{T}"/>
        /// instances; those become logically invalid after disposal (their program
        /// handle is no longer a valid GL object). The application is responsible
        /// for not calling Bind on a disposed program.
        /// </summary>
        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }
    }
}
