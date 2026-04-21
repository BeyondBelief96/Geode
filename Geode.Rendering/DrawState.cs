using Geode.Rendering.Buffers;
using Geode.Rendering.Shaders;

namespace Geode.Rendering
{
    /// <summary>
    /// Groups a <see cref="Rendering.RenderState"/>, <see cref="Shaders.ShaderProgram"/>,
    /// and <see cref="Buffers.VertexArrayObject"/> into a single drawable unit.
    /// Pass this to the renderer to execute a draw call with the correct state.
    /// </summary>
    public class DrawState
    {
        /// <summary>The GPU pipeline state (depth, culling, blending, etc.) for this draw call.</summary>
        public RenderState RenderState { get; set; }

        /// <summary>The compiled shader program to use for this draw call.</summary>
        public ShaderProgram ShaderProgram { get; set; }

        /// <summary>The VAO containing vertex/index data and attribute layout.</summary>
        public VertexArrayObject VertexArrayObject { get; set; }

        /// <summary>
        /// Creates a draw state with explicit render state, shader, and vertex array.
        /// </summary>
        /// <param name="renderState">The GPU pipeline state to apply before drawing.</param>
        /// <param name="shaderProgram">The shader program to bind before drawing.</param>
        /// <param name="vertexArrayObject">The VAO to draw.</param>
        public DrawState(RenderState renderState, ShaderProgram shaderProgram, VertexArrayObject vertexArrayObject)
        {
            RenderState = renderState;
            ShaderProgram = shaderProgram;
            VertexArrayObject = vertexArrayObject;
        }

        /// <summary>
        /// Creates a draw state with default render state.
        /// </summary>
        /// <param name="shaderProgram">The shader program to bind before drawing.</param>
        /// <param name="vertexArrayObject">The VAO to draw.</param>
        public DrawState(ShaderProgram shaderProgram, VertexArrayObject vertexArrayObject)
            : this(new RenderState(), shaderProgram, vertexArrayObject)
        {
        }
    }
}
