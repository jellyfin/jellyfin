using MediaBrowser.Common.Drawing;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MediaBrowser.Api
{
    public static class ImageProcessor
    {
        /// <summary>
        /// Resizes an image from a source stream and saves the result to an output stream
        /// </summary>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public static void ProcessImage(Stream sourceImageStream, Stream toStream, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
        {
            Image originalImage = Image.FromStream(sourceImageStream);

            Size newSize = DrawingUtils.Resize(originalImage.Size, width, height, maxWidth, maxHeight);

            Bitmap thumbnail;

            // Graphics.FromImage will throw an exception if the PixelFormat is Indexed, so we need to handle that here
            if (originalImage.PixelFormat.HasFlag(PixelFormat.Indexed))
            {
                thumbnail = new Bitmap(originalImage, newSize.Width, newSize.Height);
            }
            else
            {
                thumbnail = new Bitmap(newSize.Width, newSize.Height, originalImage.PixelFormat);
            }

            thumbnail.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            Graphics thumbnailGraph = Graphics.FromImage(thumbnail);

            thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
            thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
            thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
            thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
            thumbnailGraph.CompositingMode = CompositingMode.SourceOver;

            thumbnailGraph.DrawImage(originalImage, 0, 0, newSize.Width, newSize.Height);

            Write(originalImage, thumbnail, toStream, quality);

            thumbnailGraph.Dispose();
            thumbnail.Dispose();
            originalImage.Dispose();
        }

        private static void Write(Image originalImage, Image newImage, Stream toStream, int? quality)
        {
            // Use special save methods for jpeg and png that will result in a much higher quality image
            // All other formats use the generic Image.Save
            if (ImageFormat.Jpeg.Equals(originalImage.RawFormat))
            {
                SaveJpeg(newImage, toStream, quality);
            }
            else if (ImageFormat.Png.Equals(originalImage.RawFormat))
            {
                newImage.Save(toStream, ImageFormat.Png);
            }
            else
            {
                newImage.Save(toStream, originalImage.RawFormat);
            }
        }

        private static void SaveJpeg(Image newImage, Stream target, int? quality)
        {
            if (!quality.HasValue)
            {
                quality = 90;
            }

            using (var encoderParameters = new EncoderParameters(1))
            {
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality.Value);
                newImage.Save(target, GetImageCodeInfo("image/jpeg"), encoderParameters);
            }
        }

        private static ImageCodecInfo GetImageCodeInfo(string mimeType)
        {
            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();

            for (int i = 0; i < info.Length; i++)
            {
                ImageCodecInfo ici = info[i];
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase))
                {
                    return ici;
                }
            }
            return info[1];
        }
    }
}
