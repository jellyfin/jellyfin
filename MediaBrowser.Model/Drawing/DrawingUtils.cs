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
        /// <param name="size">The original size object</param>
        /// <param name="width">A new fixed width, if desired</param>
        /// <param name="height">A new fixed height, if desired</param>
        /// <param name="maxWidth">A max fixed width, if desired</param>
        /// <param name="maxHeight">A max fixed height, if desired</param>
        /// <returns>A new size object</returns>
        public static ImageSize Resize(ImageSize size,
            double width,
            double height,
            double maxWidth,
            double maxHeight)
        {
            double newWidth = size.Width;
            double newHeight = size.Height;

            if (width > 0 && height > 0)
            {
                newWidth = width;
                newHeight = height;
            }

            else if (height > 0)
            {
                newWidth = GetNewWidth(newHeight, newWidth, height);
                newHeight = height;
            }

            else if (width > 0)
            {
                newHeight = GetNewHeight(newHeight, newWidth, width);
                newWidth = width;
            }

            if (maxHeight > 0 && maxHeight < newHeight)
            {
                newWidth = GetNewWidth(newHeight, newWidth, maxHeight);
                newHeight = maxHeight;
            }

            if (maxWidth > 0 && maxWidth < newWidth)
            {
                newHeight = GetNewHeight(newHeight, newWidth, maxWidth);
                newWidth = maxWidth;
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
