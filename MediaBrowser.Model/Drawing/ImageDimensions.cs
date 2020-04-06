#pragma warning disable CS1591

using System.Globalization;

namespace MediaBrowser.Model.Drawing
{
    /// <summary>
    /// Struct ImageDimensions.
    /// </summary>
    public readonly struct ImageDimensions
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

        public bool Equals(ImageDimensions size)
        {
            return Width.Equals(size.Width) && Height.Equals(size.Height);
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
    }
}
