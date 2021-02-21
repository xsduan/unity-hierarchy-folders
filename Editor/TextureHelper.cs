// This software includes third-party software subject to the associated copyrights, as follows:
//
// Name: SolidUtilities
// Repo: https://github.com/SolidAlloy/SolidUtilities
// License: MIT (https://github.com/SolidAlloy/SolidUtilities/blob/master/LICENSE)
// Copyright (c) 2020 SolidAlloy

namespace UnityHierarchyFolders.Editor
{
    using System;
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary>Helps to create new textures.</summary>
    public static class TextureHelper
    {
        /// <summary>
        /// Temporarily sets <see cref="GL.sRGBWrite"/> to the passed value, then returns it back.
        /// </summary>
        [PublicAPI]
        public readonly struct SRGBWriteScope : IDisposable
        {
            private readonly bool _previousValue;

            /// <summary>Temporarily sets <see cref="GL.sRGBWrite"/> to <paramref name=""/>, then executes the action.</summary>
            /// <param name="enableWrite"> Temporary value of <see cref="GL.sRGBWrite"/>. </param>
            /// <example><code>
            /// using (new SRGBWriteScope(true))
            /// {
            ///     GL.Clear(false, true, new Color(1f, 1f, 1f, 0f));
            ///     Graphics.Blit(Default, temporary, material);
            /// });
            /// </code></example>
            public SRGBWriteScope(bool enableWrite)
            {
                _previousValue = GL.sRGBWrite;
                GL.sRGBWrite = enableWrite;
            }

            public void Dispose()
            {
                GL.sRGBWrite = _previousValue;
            }
        }

        /// <summary>
        /// Creates a temporary texture, sets it as active in <see cref="RenderTexture.active"/>, then removes the changes
        /// and sets the previous active texture back automatically.
        /// </summary>
        /// <seealso cref="TemporaryRenderTexture"/>
        /// <example><code>
        /// using (var temporaryActiveTexture = new TemporaryActiveTexture(icon.width, icon.height, 0))
        /// {
        ///     Graphics.Blit(icon, temporary, material);
        /// });
        /// </code></example>
        [PublicAPI]
        public class TemporaryActiveTexture : IDisposable
        {
            private readonly RenderTexture _previousActiveTexture;
            private readonly TemporaryRenderTexture _value;

            /// <summary>
            /// Creates a temporary texture, sets it as active in <see cref="RenderTexture.active"/>, then removes it
            /// and sets the previous active texture back automatically.
            /// </summary>
            /// <param name="width">Width of the temporary texture in pixels.</param>
            /// <param name="height">Height of the temporary texture in pixels.</param>
            /// <param name="depthBuffer">Depth buffer of the temporary texture.</param>
            /// <seealso cref="TemporaryRenderTexture"/>
            /// <example><code>
            /// using (var temporaryActiveTexture = new TemporaryActiveTexture(icon.width, icon.height, 0))
            /// {
            ///     Graphics.Blit(icon, temporary, material);
            /// });
            /// </code></example>
            public TemporaryActiveTexture(int width, int height, int depthBuffer)
            {
                _previousActiveTexture = RenderTexture.active;
                _value = new TemporaryRenderTexture(width, height, depthBuffer);
                RenderTexture.active = _value;
            }

            public static implicit operator RenderTexture(TemporaryActiveTexture temporaryTexture) => temporaryTexture._value;

            public void Dispose()
            {
                _value.Dispose();
                RenderTexture.active = _previousActiveTexture;
            }
        }

        /// <summary>Creates a temporary texture that can be used and then removed automatically.</summary>
        /// <seealso cref="TemporaryActiveTexture"/>
        /// <example><code>
        /// using (var temporaryTexture = new TemporaryRenderTexture(icon.width, icon.height, 0))
        /// {
        ///     Graphics.Blit(icon, temporaryTexture, material);
        /// });
        /// </code></example>
        [PublicAPI]
        public class TemporaryRenderTexture : IDisposable
        {
            private readonly RenderTexture _value;

            /// <summary>Creates a temporary texture that can be used and then removed automatically.</summary>
            /// <param name="width">Width of the temporary texture in pixels.</param>
            /// <param name="height">Height of the temporary texture in pixels.</param>
            /// <param name="depthBuffer">Depth buffer of the temporary texture.</param>
            /// <seealso cref="TemporaryActiveTexture"/>
            /// <example><code>
            /// using (var temporaryTexture = new TemporaryRenderTexture(icon.width, icon.height, 0))
            /// {
            ///     Graphics.Blit(icon, temporaryTexture, material);
            /// });
            /// </code></example>
            public TemporaryRenderTexture(int width, int height, int depthBuffer)
            {
                _value = RenderTexture.GetTemporary(width, height, depthBuffer);
            }

            public static implicit operator RenderTexture(TemporaryRenderTexture temporaryRenderTexture) => temporaryRenderTexture._value;

            public void Dispose()
            {
                RenderTexture.ReleaseTemporary(_value);
            }
        }
    }
}