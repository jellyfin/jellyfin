using System;

namespace Jellyfin.Drawing.Skia
{
    /// <summary>
    /// Represents errors that occur during interaction with Skia.
    /// </summary>
    public class SkiaException : Exception
    {
        /// <inheritdoc />
        public SkiaException() : base()
        {
        }

        /// <inheritdoc />
        public SkiaException(string message) : base(message)
        {
        }

        /// <inheritdoc />
        public SkiaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
