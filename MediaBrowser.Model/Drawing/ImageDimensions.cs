#pragma warning disable CS1591

using System;
using System.Diagnostics.CodeAnalysis;
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

        public override bool Equals([AllowNull] object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((ImageDimensions)obj);
        }

        public bool Equals(ImageDimensions other)
        {
            return Width.Equals(other.Width) && Height.Equals(other.Height);
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

        public override int GetHashCode()
        {
            return (Height.GetHashCode() * 17) + Width.GetHashCode();
        }
    }
}
