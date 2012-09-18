using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MediaBrowser.Controller.Drawing
{
    public static class ImageProcessor
    {
        /// <summary>
        /// Processes an image by resizing to target dimensions
        /// </summary>
        /// <param name="sourceImageStream">The stream containing the source image</param>
        /// <param name="toStream">The stream to save the new image to</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        /// <param name="entity">The entity that owns the image</param>
        /// <param name="imageType">The image type</param>
        /// <param name="imageIndex">The image index (currently only used with backdrops)</param>
        public static void ProcessImage(Stream sourceImageStream, Stream toStream, int? width, int? height, int? maxWidth, int? maxHeight, int? quality, BaseEntity entity, ImageType imageType, int imageIndex)
        {
            Image originalImage = Image.FromStream(sourceImageStream);

            // Determine the output size based on incoming parameters
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

            thumbnail.MakeTransparent();

            // Preserve the original resolution
            thumbnail.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            Graphics thumbnailGraph = Graphics.FromImage(thumbnail);

            thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
            thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
            thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
            thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
            thumbnailGraph.CompositingMode = CompositingMode.SourceOver;

            thumbnailGraph.DrawImage(originalImage, 0, 0, newSize.Width, newSize.Height);

            ImageFormat outputFormat = originalImage.RawFormat;

            // Run Kernel image processors
            if (Kernel.Instance.ImageProcessors.Any())
            {
                ExecuteAdditionalImageProcessors(originalImage, thumbnail, thumbnailGraph, entity, imageType, imageIndex);

                if (Kernel.Instance.ImageProcessors.Any(i => i.RequiresTransparency))
                {
                    outputFormat = ImageFormat.Png;
                }
            }

            // Write to the output stream
            SaveImage(outputFormat, thumbnail, toStream, quality);

            thumbnailGraph.Dispose();
            thumbnail.Dispose();
            originalImage.Dispose();
        }

        /// <summary>
        /// Executes additional image processors that are registered with the Kernel
        /// </summary>
        /// <param name="bitmap">The bitmap holding the original image, after re-sizing</param>
        /// <param name="graphics">The graphics surface on which the output is drawn</param>
        /// <param name="entity">The entity that owns the image</param>
        /// <param name="imageType">The image type</param>
        /// <param name="imageIndex">The image index (currently only used with backdrops)</param>
        private static void ExecuteAdditionalImageProcessors(Image originalImage, Bitmap bitmap, Graphics graphics, BaseEntity entity, ImageType imageType, int imageIndex)
        {
            var baseItem = entity as BaseItem;

            if (baseItem != null)
            {
                foreach (var processor in Kernel.Instance.ImageProcessors)
                {
                    processor.ProcessImage(originalImage, bitmap, graphics, baseItem, imageType, imageIndex);
                }
            }
            else
            {
                foreach (var processor in Kernel.Instance.ImageProcessors)
                {
                    processor.ProcessImage(originalImage, bitmap, graphics, entity);
                }
            }
        }

        public static void SaveImage(ImageFormat outputFormat, Image newImage, Stream toStream, int? quality)
        {
            // Use special save methods for jpeg and png that will result in a much higher quality image
            // All other formats use the generic Image.Save
            if (ImageFormat.Jpeg.Equals(outputFormat))
            {
                SaveJpeg(newImage, toStream, quality);
            }
            else if (ImageFormat.Png.Equals(outputFormat))
            {
                newImage.Save(toStream, ImageFormat.Png);
            }
            else
            {
                newImage.Save(toStream, outputFormat);
            }
        }

        public static void SaveJpeg(Image image, Stream target, int? quality)
        {
            if (!quality.HasValue)
            {
                quality = 90;
            }

            using (var encoderParameters = new EncoderParameters(1))
            {
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality.Value);
                image.Save(target, GetImageCodecInfo("image/jpeg"), encoderParameters);
            }
        }

        public static ImageCodecInfo GetImageCodecInfo(string mimeType)
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
