using Geode.Rendering.Shaders.Uniforms;
using Geode.Rendering.Uniforms;
using System.Collections.Generic;

namespace Geode.Rendering.Shaders
{
    /// <summary>
    /// Abstract base for one GLSL uniform in a linked shader program.
    /// Clients do not use this type directly -- cast to <see cref="Uniform{T}"/>
    /// to read or write the value.
    /// </summary>
    public abstract class Uniform : ICleanable
    {
        /// <summary>The GLSL uniform name as declared in the shader source.</summary>
        public string Name { get; }

        /// <summary>The GLSL type. Used to dispatch to the correct concrete subclass.</summary>
        public UniformType Datatype { get; }

        protected Uniform(string name, UniformType datatype)
        {
            Name = name;
            Datatype = datatype;
        }

        /// <summary>Flush this uniform's cached value to the GPU. Called by ShaderProgram.</summary>
        public abstract void Clean();
    }

    /// <summary>
    /// A strongly typed uniform. Concrete subclasses (<see cref="GL.UniformFloatMatrix44GL"/>, etc.)
    /// override <see cref="Uniform.Clean"/> to call the appropriate glUniform* function.
    /// </summary>
    /// <typeparam name="T">CPU-side value type (float, Vector3, Matrix4x4, int, ...).</typeparam>
    public abstract class Uniform<T> : Uniform
    {
        private T _value;
        private readonly ICleanableObserver _observer;
        private bool _dirty;

        protected Uniform(string name, UniformType type, ICleanableObserver observer) : base(name, type) 
        {
            _observer = observer;
            _value = default!;
            _dirty = true; // Mark dirty initially to ensure the first value is sent to the GPU
            observer.NotifyDirty(this);
        }

        /// <summary>
        /// The cached CPU-side value. Setting a different value marks the uniform dirty
        /// (scheduled for GPU upload before the next draw). Setting the same value is a no-op.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;
                _value = value;

                if(!_dirty)
                {
                    _dirty = true;
                    _observer.NotifyDirty(this);
                }
            }
        }

        /// <summary>
        /// For concrete subclasses: read the cached value without a dirty check.
        /// </summary>
        protected T CurrentValue => _value;

        /// <summary>
        /// For concrete subclasses: clear the dirty
        /// </summary>
        protected void MarkClean() => _dirty = false;
    }
}
