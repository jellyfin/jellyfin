using System;

namespace MediaBrowser.Model.Drawing
{
    /// <summary>
    /// Class DrawingUtils.
    /// </summary>
    public static class DrawingUtils
    {
        /// <summary>
        /// Resizes a set of dimensions.
        /// </summary>
        /// <param name="size">The original size object.</param>
        /// <param name="width">A new fixed width, if desired.</param>
        /// <param name="height">A new fixed height, if desired.</param>
        /// <param name="maxWidth">A max fixed width, if desired.</param>
        /// <param name="maxHeight">A max fixed height, if desired.</param>
        /// <returns>A new size object.</returns>
        public static ImageDimensions Resize(
            ImageDimensions size,
            int width,
            int height,
            int maxWidth,
            int maxHeight)
        {
            int newWidth = size.Width;
            int newHeight = size.Height;

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

            return new ImageDimensions(newWidth, newHeight);
        }

        /// <summary>
        /// Gets the new width.
        /// </summary>
        /// <param name="currentHeight">Height of the current.</param>
        /// <param name="currentWidth">Width of the current.</param>
        /// <param name="newHeight">The new height.</param>
        /// <returns>The new width.</returns>
        private static int GetNewWidth(int currentHeight, int currentWidth, int newHeight)
            => Convert.ToInt32((double)newHeight / currentHeight * currentWidth);

        /// <summary>
        /// Gets the new height.
        /// </summary>
        /// <param name="currentHeight">Height of the current.</param>
        /// <param name="currentWidth">Width of the current.</param>
        /// <param name="newWidth">The new width.</param>
        /// <returns>System.Double.</returns>
        private static int GetNewHeight(int currentHeight, int currentWidth, int newWidth)
            => Convert.ToInt32((double)newWidth / currentWidth * currentHeight);
    }
}
