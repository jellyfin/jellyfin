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
            return Scale(new ImageSize
            {
                Width = currentWidth, 
                Height = currentHeight

            }, scaleFactor);
        }

        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="scaleFactor">The scale factor.</param>
        /// <returns>ImageSize.</returns>
        public static ImageSize Scale(ImageSize size, double scaleFactor)
        {
            double newWidth = size.Width * scaleFactor;

            return Resize(size.Width, size.Height, newWidth, null, null, null);
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
        public static ImageSize Resize(double currentWidth, 
            double currentHeight, 
            double? width, 
            double? height, 
            double? maxWidth,
            double? maxHeight)
        {
            return Resize(new ImageSize
            {
                Width = currentWidth, 
                Height = currentHeight

            }, width, height, maxWidth, maxHeight);
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
        public static ImageSize Resize(ImageSize size, 
            double? width, 
            double? height, 
            double? maxWidth, 
            double? maxHeight)
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

            if (maxHeight.HasValue && maxHeight.Value < newHeight)
            {
                newWidth = GetNewWidth(newHeight, newWidth, maxHeight.Value);
                newHeight = maxHeight.Value;
            }

            if (maxWidth.HasValue && maxWidth.Value < newWidth)
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
            double scaleFactor = newHeight;
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
            double scaleFactor = newWidth;
            scaleFactor /= currentWidth;
            scaleFactor *= currentHeight;

            return scaleFactor;
        }
    }
}
