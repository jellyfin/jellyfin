using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MediaBrowser.Api
{
    public static class ImageProcessor
    {
        public static void ProcessImage(string path, Stream toStream, int? width, int? height, int? maxWidth, int? maxHeight, int? quality)
        {
            Image originalImage = Image.FromFile(path);

            var newWidth = originalImage.Width;
            var newHeight = originalImage.Height;

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

            Bitmap thumbnail = new Bitmap(newWidth, newHeight, originalImage.PixelFormat);
            thumbnail.SetResolution(originalImage.HorizontalResolution, originalImage.VerticalResolution);

            Graphics thumbnailGraph = Graphics.FromImage(thumbnail);
            thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
            thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
            thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;
            thumbnailGraph.PixelOffsetMode = PixelOffsetMode.HighQuality;
            thumbnailGraph.CompositingMode = CompositingMode.SourceOver;

            thumbnailGraph.DrawImage(originalImage, 0, 0, newWidth, newHeight);

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
                SavePng(newImage, toStream);
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

            using (EncoderParameters encoderParameters = new EncoderParameters(1))
            {
                encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality.Value);
                newImage.Save(target, GetImageCodeInfo("image/jpeg"), encoderParameters);
            }
        }

        private static void SavePng(Image newImage, Stream target)
        {
            if (target.CanSeek)
            {
                newImage.Save(target, ImageFormat.Png);
            }
            else
            {
                using (MemoryStream ms = new MemoryStream(4096))
                {
                    newImage.Save(ms, ImageFormat.Png);
                    ms.WriteTo(target);
                }
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

        private static int GetNewWidth(int currentHeight, int currentWidth, int newHeight)
        {
            decimal scaleFactor = newHeight;
            scaleFactor /= currentHeight;
            scaleFactor *= currentWidth;

            return Convert.ToInt32(scaleFactor);
        }

        private static int GetNewHeight(int currentHeight, int currentWidth, int newWidth)
        {
            decimal scaleFactor = newWidth;
            scaleFactor /= currentWidth;
            scaleFactor *= currentHeight;

            return Convert.ToInt32(scaleFactor);
        }
    }
}
