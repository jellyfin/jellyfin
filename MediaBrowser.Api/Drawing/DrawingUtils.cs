using System;
using System.Drawing;

namespace MediaBrowser.Api.Drawing
{
    public static class DrawingUtils
    {
        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        public static Size Resize(int currentWidth, int currentHeight, int? width, int? height, int? maxWidth, int? maxHeight)
        {
            return Resize(new Size(currentWidth, currentHeight), width, height, maxWidth, maxHeight);
        }

        /// <summary>
        /// Resizes a set of dimensions
        /// </summary>
        /// <param name="size">The original size object</param>
        /// <param name="width">A new fixed width, if desired</param>
        /// <param name="height">A new fixed neight, if desired</param>
        /// <param name="maxWidth">A max fixed width, if desired</param>
        /// <param name="maxHeight">A max fixed height, if desired</param>
        /// <returns>A new size object</returns>
        public static Size Resize(Size size, int? width, int? height, int? maxWidth, int? maxHeight)
        {
            decimal newWidth = size.Width;
            decimal newHeight = size.Height;

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

            return new Size(Convert.ToInt32(newWidth), Convert.ToInt32(newHeight));
        }

        private static decimal GetNewWidth(decimal currentHeight, decimal currentWidth, int newHeight)
        {
            decimal scaleFactor = newHeight;
            scaleFactor /= currentHeight;
            scaleFactor *= currentWidth;

            return scaleFactor;
        }

        private static decimal GetNewHeight(decimal currentHeight, decimal currentWidth, int newWidth)
        {
            decimal scaleFactor = newWidth;
            scaleFactor /= currentWidth;
            scaleFactor *= currentHeight;

            return scaleFactor;
        }
    }
}
