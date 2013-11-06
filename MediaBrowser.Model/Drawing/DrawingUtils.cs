
namespace MediaBrowser.Model.Drawing
{
    /// <summary>
    /// Class DrawingUtils
    /// </summary>
    public static class DrawingUtils
    {
        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        /// <param name="currentWidth">Width of the current.</param>
        /// <param name="currentHeight">Height of the current.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>ImageSize.</returns>
        public static ImageSize Scale(double currentWidth, double currentHeight, double scaleFactor)
        {
            return Scale(new ImageSize { Width = currentWidth, Height = currentHeight }, scaleFactor);
        }

        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>ImageSize.</returns>
        public static ImageSize Scale(ImageSize size, double scaleFactor)
        {
            var newWidth = size.Width * scaleFactor;

            return Resize(size.Width, size.Height, newWidth);
        }

        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        /// <param name="currentWidth">Width of the current.</param>
        /// <param name="currentHeight">Height of the current.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="maxWidth">A max fixed width, if desired</param>
        /// <param name="maxHeight">A max fixed height, if desired</param>
        /// <returns>ImageSize.</returns>
        public static ImageSize Resize(double currentWidth, double currentHeight, double? width = null, double? height = null, double? maxWidth = null, double? maxHeight = null)
        {
            return Resize(new ImageSize { Width = currentWidth, Height = currentHeight }, width, height, maxWidth, maxHeight);
        }

        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        /// <param name="size">The original size object</param>
        /// <param name="width">A new fixed width, if desired</param>
        /// <param name="height">A new fixed height, if desired</param>
        /// <param name="maxWidth">A max fixed width, if desired</param>
        /// <param name="maxHeight">A max fixed height, if desired</param>
        /// <returns>A new size object</returns>
        public static ImageSize Resize(ImageSize size, double? width = null, double? height = null, double? maxWidth = null, double? maxHeight = null)
        {
            double newWidth = size.Width;
            double newHeight = size.Height;

            if (width.HasValue && height.HasValue)
            {
                newWidth = width.Value;
                newHeight = height.Value;
            }

            else if (height.HasValue)
            {
                newWidth = GetNewWidth(newHeight, newWidth, height.Value);
                newHeight = height.Value;
            }

            else if (width.HasValue)
            {
                newHeight = GetNewHeight(newHeight, newWidth, width.Value);
                newWidth = width.Value;
            }

            if (maxHeight.HasValue && maxHeight < newHeight)
            {
                newWidth = GetNewWidth(newHeight, newWidth, maxHeight.Value);
                newHeight = maxHeight.Value;
            }

            if (maxWidth.HasValue && maxWidth < newWidth)
            {
                newHeight = GetNewHeight(newHeight, newWidth, maxWidth.Value);
                newWidth = maxWidth.Value;
            }

            return new ImageSize { Width = newWidth, Height = newHeight };
        }

        /// <summary>
        /// Gets the new width.
        /// </summary>
        /// <param name="currentHeight">Height of the current.</param>
        /// <param name="currentWidth">Width of the current.</param>
        /// <param name="newHeight">The new height.</param>
        /// <returns>System.Double.</returns>
        private static double GetNewWidth(double currentHeight, double currentWidth, double newHeight)
        {
            var scaleFactor = newHeight;
            scaleFactor /= currentHeight;
            scaleFactor *= currentWidth;

            return scaleFactor;
        }

        /// <summary>
        /// Gets the new height.
        /// </summary>
        /// <param name="currentHeight">Height of the current.</param>
        /// <param name="currentWidth">Width of the current.</param>
        /// <param name="newWidth">The new width.</param>
        /// <returns>System.Double.</returns>
        private static double GetNewHeight(double currentHeight, double currentWidth, double newWidth)
        {
            var scaleFactor = newWidth;
            scaleFactor /= currentWidth;
            scaleFactor *= currentHeight;

            return scaleFactor;
        }
    }

    /// <summary>
    /// Struct ImageSize
    /// </summary>
    public struct ImageSize
    {
        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height { get; set; }
        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public double Width { get; set; }

        public bool Equals(ImageSize size)
        {
            return Width.Equals(size.Width) && Height.Equals(size.Height);
        }
    }
}
