using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Emby.Drawing.Net
{
    public static class ImageExtensions
    {
        /// <summary>
        /// Saves the image.
        /// </summary>
        /// <param name="outputFormat">The output format.</param>
        /// <param name="image">The image.</param>
        /// <param name="toStream">To stream.</param>
        /// <param name="quality">The quality.</param>
        public static void Save(this Image image, ImageFormat outputFormat, Stream toStream, int quality)
        {
            // Use special save methods for jpeg and png that will result in a much higher quality image
            // All other formats use the generic Image.Save
            if (ImageFormat.Jpeg.Equals(outputFormat))
            {
                SaveAsJpeg(image, toStream, quality);
            }
            else if (ImageFormat.Png.Equals(outputFormat))
            {
                image.Save(toStream, ImageFormat.Png);
            }
            else
            {
                image.Save(toStream, outputFormat);
            }
        }

        /// <summary>
        /// Saves the JPEG.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="target">The target.</param>
        /// <param name="quality">The quality.</param>
        public static void SaveAsJpeg(this Image image, Stream target, int quality)
        {
            using (var encoderParameters = new EncoderParameters(1))
            {
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                image.Save(target, GetImageCodecInfo("image/jpeg"), encoderParameters);
            }
        }

        private static readonly ImageCodecInfo[] Encoders = ImageCodecInfo.GetImageEncoders();

        /// <summary>
        /// Gets the image codec info.
        /// </summary>
        /// <param name="mimeType">Type of the MIME.</param>
        /// <returns>ImageCodecInfo.</returns>
        private static ImageCodecInfo GetImageCodecInfo(string mimeType)
        {
            foreach (var encoder in Encoders)
            {
                if (string.Equals(encoder.MimeType, mimeType, StringComparison.OrdinalIgnoreCase))
                {
                    return encoder;
                }
            }

            return Encoders.Length == 0 ? null : Encoders[0];
        }

        /// <summary>
        /// Crops an image by removing whitespace and transparency from the edges
        /// </summary>
        /// <param name="bmp">The BMP.</param>
        /// <returns>Bitmap.</returns>
        /// <exception cref="System.Exception"></exception>
        public static Bitmap CropWhitespace(this Bitmap bmp)
        {
            var width = bmp.Width;
            var height = bmp.Height;

            var topmost = 0;
            for (int row = 0; row < height; ++row)
            {
                if (IsAllWhiteRow(bmp, row, width))
                    topmost = row;
                else break;
            }

            int bottommost = 0;
            for (int row = height - 1; row >= 0; --row)
            {
                if (IsAllWhiteRow(bmp, row, width))
                    bottommost = row;
                else break;
            }

            int leftmost = 0, rightmost = 0;
            for (int col = 0; col < width; ++col)
            {
                if (IsAllWhiteColumn(bmp, col, height))
                    leftmost = col;
                else
                    break;
            }

            for (int col = width - 1; col >= 0; --col)
            {
                if (IsAllWhiteColumn(bmp, col, height))
                    rightmost = col;
                else
                    break;
            }

            if (rightmost == 0) rightmost = width; // As reached left
            if (bottommost == 0) bottommost = height; // As reached top.

            var croppedWidth = rightmost - leftmost;
            var croppedHeight = bottommost - topmost;

            if (croppedWidth == 0) // No border on left or right
            {
                leftmost = 0;
                croppedWidth = width;
            }

            if (croppedHeight == 0) // No border on top or bottom
            {
                topmost = 0;
                croppedHeight = height;
            }

            // Graphics.FromImage will throw an exception if the PixelFormat is Indexed, so we need to handle that here
            var thumbnail = new Bitmap(croppedWidth, croppedHeight, PixelFormat.Format32bppPArgb);

            // Preserve the original resolution
            TrySetResolution(thumbnail, bmp.HorizontalResolution, bmp.VerticalResolution);

            using (var thumbnailGraph = Graphics.FromImage(thumbnail))
            {
                thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
                thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
                thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
                thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
                thumbnailGraph.CompositingMode = CompositingMode.SourceCopy;

                thumbnailGraph.DrawImage(bmp,
                  new RectangleF(0, 0, croppedWidth, croppedHeight),
                  new RectangleF(leftmost, topmost, croppedWidth, croppedHeight),
                  GraphicsUnit.Pixel);
            }
            return thumbnail;
        }

        /// <summary>
        /// Tries the set resolution.
        /// </summary>
        /// <param name="bmp">The BMP.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private static void TrySetResolution(Bitmap bmp, float x, float y)
        {
            if (x > 0 && y > 0)
            {
                bmp.SetResolution(x, y);
            }
        }

        /// <summary>
        /// Determines whether or not a row of pixels is all whitespace
        /// </summary>
        /// <param name="bmp">The BMP.</param>
        /// <param name="row">The row.</param>
        /// <param name="width">The width.</param>
        /// <returns><c>true</c> if [is all white row] [the specified BMP]; otherwise, <c>false</c>.</returns>
        private static bool IsAllWhiteRow(Bitmap bmp, int row, int width)
        {
            for (var i = 0; i < width; ++i)
            {
                if (!IsWhiteSpace(bmp.GetPixel(i, row)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether or not a column of pixels is all whitespace
        /// </summary>
        /// <param name="bmp">The BMP.</param>
        /// <param name="col">The col.</param>
        /// <param name="height">The height.</param>
        /// <returns><c>true</c> if [is all white column] [the specified BMP]; otherwise, <c>false</c>.</returns>
        private static bool IsAllWhiteColumn(Bitmap bmp, int col, int height)
        {
            for (var i = 0; i < height; ++i)
            {
                if (!IsWhiteSpace(bmp.GetPixel(col, i)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if a color is whitespace
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns><c>true</c> if [is white space] [the specified color]; otherwise, <c>false</c>.</returns>
        private static bool IsWhiteSpace(Color color)
        {
            return (color.R == 255 && color.G == 255 && color.B == 255) || color.A == 0;
        }
    }
}
