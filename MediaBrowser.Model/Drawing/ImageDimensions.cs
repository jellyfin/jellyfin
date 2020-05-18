#pragma warning disable CS1591

using System;
using System.Globalization;

namespace MediaBrowser.Model.Drawing
{
    /// <summary>
    /// Struct ImageDimensions.
    /// </summary>
    public readonly struct ImageDimensions : IEquatable<ImageDimensions>
    {
        public ImageDimensions(int width, int height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        public int Height { get; }

        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>The width.</value>
        public int Width { get; }

        public static bool operator ==(ImageDimensions left, ImageDimensions right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageDimensions left, ImageDimensions right)
        {
            return !(left == right);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}-{1}",
                Width,
                Height);
        }

        public override bool Equals(object obj)
        {
            return obj is ImageDimensions other
                && Width == other.Width
                && Height == other.Height;
        }

        public bool Equals(ImageDimensions other)
        {
            return Width == other.Width
                && Height == other.Height;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Width.GetHashCode() * 397) + Height.GetHashCode();
            }
        }
    }
}
