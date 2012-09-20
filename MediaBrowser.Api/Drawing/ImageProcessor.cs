using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace MediaBrowser.Api.Drawing
{
    public static class ImageProcessor
    {
        /// <summary>
        /// Processes an image by resizing to target dimensions
        /// </summary>
        /// <param name="entity">The entity that owns the image</param>
        /// <param name="imageType">The image type</param>
        /// <param name="imageIndex">The image index (currently only used with backdrops)</param>
        /// <param name="toStream">The stream to save the new image to</param>
        /// <param name="width">Use if a fixed width is required. Aspect ratio will be preserved.</param>
        /// <param name="height">Use if a fixed height is required. Aspect ratio will be preserved.</param>
        /// <param name="maxWidth">Use if a max width is required. Aspect ratio will be preserved.</param>
        /// <param name="maxHeight">Use if a max height is required. Aspect ratio will be preserved.</param>
        /// <param name="quality">Quality level, from 0-100. Currently only applies to JPG. The default value should suffice.</param>
        public static void ProcessImage(BaseEntity entity, ImageType imageType, int imageIndex, Stream toStream, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
        {
            Image originalImage = Image.FromFile(GetImagePath(entity, imageType, imageIndex));

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

            // Write to the output stream
            SaveImage(outputFormat, thumbnail, toStream, quality);

            thumbnailGraph.Dispose();
            thumbnail.Dispose();
            originalImage.Dispose();
        }

        public static string GetImagePath(BaseEntity entity, ImageType imageType, int imageIndex)
        {
            var item = entity as BaseItem;

            if (item != null)
            {
                if (imageType == ImageType.Logo)
                {
                    return item.LogoImagePath;
                }
                if (imageType == ImageType.Backdrop)
                {
                    return item.BackdropImagePaths.ElementAt(imageIndex);
                }
                if (imageType == ImageType.Banner)
                {
                    return item.BannerImagePath;
                }
                if (imageType == ImageType.Art)
                {
                    return item.ArtImagePath;
                }
                if (imageType == ImageType.Thumbnail)
                {
                    return item.ThumbnailImagePath;
                }
            }

            return entity.PrimaryImagePath;
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
