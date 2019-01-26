using System;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;

namespace Emby.Drawing
{
    public class NullImageEncoder : IImageEncoder
    {
        public string[] SupportedInputFormats =>
            new[]
            {
                "png",
                "jpeg",
                "jpg"
            };

        public ImageFormat[] SupportedOutputFormats => new[] { ImageFormat.Jpg, ImageFormat.Png };

        public void CropWhiteSpace(string inputPath, string outputPath)
        {
            throw new NotImplementedException();
        }

        public string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            throw new NotImplementedException();
        }

        public void CreateImageCollage(ImageCollageOptions options)
        {
            throw new NotImplementedException();
        }

        public string Name => "Null Image Encoder";

        public bool SupportsImageCollageCreation => false;

        public bool SupportsImageEncoding => false;

        public ImageDimensions GetImageSize(string path)
        {
            throw new NotImplementedException();
        }
    }
}
