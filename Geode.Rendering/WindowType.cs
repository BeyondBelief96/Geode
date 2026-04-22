namespace Geode.Rendering
{
    /// <summary>
    /// Initial window placement style for <see cref="Device.CreateWindow"/>.
    /// Book §2.
    /// </summary>
    public enum WindowType
    {
        /// <summary>Regular windowed mode at the requested size.</summary>
        Default,

        /// <summary>Borderless full-screen on the primary monitor.</summary>
        FullScreen
    }
}
