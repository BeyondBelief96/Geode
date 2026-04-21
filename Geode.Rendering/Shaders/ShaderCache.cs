// Purpose (two jobs):
//
//   1. Avoid recompiling the same shader source when multiple draws share it.
//      Tile renderers, billboard batches, and any scene with repeated geometry
//      benefit immediately -- N draws using one shader means one GL program
//      handle, not N.
//
//   2. Enable sort-by-state (Book Section 3.3.6). The sort function buckets
//      draws by shader via REFERENCE EQUALITY of ShaderProgram, which only
//      works if two draws with identical shader source hold the same
//      ShaderProgram instance. That identity requires a cache.
//
// Ownership model: the cache owns every ShaderProgram it hands out. Callers
// do NOT Dispose the returned programs directly -- they call Release when
// they no longer need them, and the cache disposes when the refcount hits
// zero. The cache's own Dispose disposes every remaining program.
//
// Thread safety: all public methods are protected by a coarse-grained lock,
// matching the book's design. Lookups from worker threads (tile preparation)
// are safe. ShaderProgram CONSTRUCTION inside FindOrAdd, however, calls GL
// functions (glCreateShader, glLinkProgram) which must run on the render
// thread. Callers invoking FindOrAdd with new sources must do so on the
// render thread. Find() is safe from any thread.

using System;
using System.Collections.Generic;
using System.IO;

namespace Geode.Rendering.Shaders
{
    /// <summary>
    /// Reference-counted cache of compiled <see cref="ShaderProgram"/> instances keyed by
    /// an application-supplied string. See file header for the full model.
    /// </summary>
    public sealed class ShaderCache : IDisposable
    {
        private readonly Silk.NET.OpenGL.GL _gl;
        private readonly Dictionary<string, Entry> _byKey = new();
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>Create a cache bound to the given GL context.</summary>
        public ShaderCache(Silk.NET.OpenGL.GL gl)
        {
            _gl = gl ?? throw new ArgumentNullException(nameof(gl));
        }

        /// <summary>
        /// Peek at the cache without changing reference counts.
        /// Returns the existing program if one is cached under <paramref name="key"/>, otherwise null.
        /// Useful for procedural-shader code paths that want to skip expensive source
        /// generation when the compiled result is already cached.
        /// </summary>
        public ShaderProgram? Find(string key)
        {
            ThrowIfNullOrEmpty(key);
            lock (_lock)
            {
                ThrowIfDisposed();
                return _byKey.TryGetValue(key, out Entry entry) ? entry.Program : null;
            }
        }

        /// <summary>
        /// Look up a shader by key; if not present, compile a new one from the given sources
        /// and cache it. In both cases the reference count for <paramref name="key"/> is incremented.
        /// The caller must pair every FindOrAdd with a matching <see cref="Release"/>.
        /// </summary>
        /// <param name="key">Application-chosen identity for this shader (e.g. "tiled-globe/v1").</param>
        /// <param name="vertexSource">GLSL vertex shader source. Used only on a cache miss.</param>
        /// <param name="fragmentSource">GLSL fragment shader source. Used only on a cache miss.</param>
        /// <remarks>
        /// On a cache hit the source strings are ignored. If you need to avoid paying the cost
        /// of generating / reading source on every call, use the overload that takes
        /// <see cref="Func{TResult}"/> delegates for the sources -- those only invoke on miss.
        /// </remarks>
        public ShaderProgram FindOrAdd(string key, string vertexSource, string fragmentSource)
        {
            ThrowIfNullOrEmpty(key);
            if (vertexSource is null) throw new ArgumentNullException(nameof(vertexSource));
            if (fragmentSource is null) throw new ArgumentNullException(nameof(fragmentSource));

            lock (_lock)
            {
                ThrowIfDisposed();
                if (_byKey.TryGetValue(key, out Entry entry))
                {
                    entry.RefCount++;
                    _byKey[key] = entry;
                    return entry.Program;
                }

                ShaderProgram compiled = new ShaderProgram(_gl, vertexSource, fragmentSource);
                _byKey[key] = new Entry(compiled, 1);
                return compiled;
            }
        }

        /// <summary>
        /// FindOrAdd variant that defers source construction to the cache-miss path only.
        /// Preferred when reading sources is expensive (disk I/O, procedural generation).
        /// </summary>
        public ShaderProgram FindOrAdd(string key, Func<string> vertexSource, Func<string> fragmentSource)
        {
            ThrowIfNullOrEmpty(key);
            if (vertexSource is null) throw new ArgumentNullException(nameof(vertexSource));
            if (fragmentSource is null) throw new ArgumentNullException(nameof(fragmentSource));

            lock (_lock)
            {
                ThrowIfDisposed();
                if (_byKey.TryGetValue(key, out Entry entry))
                {
                    entry.RefCount++;
                    _byKey[key] = entry;
                    return entry.Program;
                }

                ShaderProgram compiled = new ShaderProgram(_gl, vertexSource(), fragmentSource());
                _byKey[key] = new Entry(compiled, 1);
                return compiled;
            }
        }

        /// <summary>
        /// Convenience for file-backed shaders. Derives a key from the two file paths so
        /// identical paths share a cached program automatically.
        /// </summary>
        public ShaderProgram FindOrAddFromFiles(string vertexPath, string fragmentPath)
        {
            if (vertexPath is null) throw new ArgumentNullException(nameof(vertexPath));
            if (fragmentPath is null) throw new ArgumentNullException(nameof(fragmentPath));

            string key = $"file:{vertexPath}|{fragmentPath}";
            // Use the deferred overload so files are only read on a miss.
            return FindOrAdd(key,
                () => File.ReadAllText(vertexPath),
                () => File.ReadAllText(fragmentPath));
        }

        /// <summary>
        /// Decrement the reference count for <paramref name="key"/>. When it reaches zero the cached
        /// program is disposed and removed from the cache. Silent no-op if the key is not present
        /// (double-release is safe, though indicative of a bug).
        /// </summary>
        /// <remarks>
        /// Must be called on the render thread -- dropping the last reference triggers
        /// <see cref="ShaderProgram.Dispose"/>, which calls <c>glDeleteProgram</c>.
        /// </remarks>
        public void Release(string key)
        {
            ThrowIfNullOrEmpty(key);

            ShaderProgram? toDispose = null;
            lock (_lock)
            {
                ThrowIfDisposed();
                if (!_byKey.TryGetValue(key, out Entry entry)) return;

                entry.RefCount--;
                if (entry.RefCount <= 0)
                {
                    toDispose = entry.Program;
                    _byKey.Remove(key);
                }
                else
                {
                    _byKey[key] = entry;
                }
            }

            // Dispose outside the lock. This matters less for ShaderProgram (glDeleteProgram
            // is fast), but it's the right discipline -- keeps the lock as short as possible
            // and avoids deadlocks if Dispose ever calls back into anything lock-dependent.
            toDispose?.Dispose();
        }

        /// <summary>The number of distinct shader programs currently cached.</summary>
        public int Count
        {
            get { lock (_lock) { return _byKey.Count; } }
        }

        /// <summary>
        /// Dispose every cached program and clear the cache. After disposal every public
        /// method throws <see cref="ObjectDisposedException"/>. Must be called on the render
        /// thread (disposing programs calls <c>glDeleteProgram</c>).
        /// </summary>
        public void Dispose()
        {
            List<ShaderProgram> toDispose;
            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;

                toDispose = new List<ShaderProgram>(_byKey.Count);
                foreach (Entry entry in _byKey.Values) toDispose.Add(entry.Program);
                _byKey.Clear();
            }

            foreach (ShaderProgram program in toDispose) program.Dispose();
        }

        // ---------------------------------------------------------------
        // Internals
        // ---------------------------------------------------------------

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ShaderCache));
        }

        private static void ThrowIfNullOrEmpty(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Shader cache key must be a non-empty string.", nameof(key));
        }

        /// <summary>
        /// One dictionary value: the compiled program and its current reference count.
        /// Stored as a struct so modifications require assigning back into the dictionary;
        /// this keeps reads cheap and writes explicit.
        /// </summary>
        private struct Entry
        {
            public ShaderProgram Program;
            public int RefCount;

            public Entry(ShaderProgram program, int refCount)
            {
                Program = program;
                RefCount = refCount;
            }
        }
    }
}
