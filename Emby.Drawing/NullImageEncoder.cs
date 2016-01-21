using System;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;

namespace Emby.Drawing
{
    public class NullImageEncoder : IImageEncoder
    {
        public string[] SupportedInputFormats
        {
            get
            {
                return new[]
                {
                    "png",
                    "jpeg",
                    "jpg"
                };
            }
        }

        public ImageFormat[] SupportedOutputFormats
        {
            get
            {
                return new[] { ImageFormat.Jpg, ImageFormat.Png };
            }
        }

        public void CropWhiteSpace(string inputPath, string outputPath)
        {
            throw new NotImplementedException();
        }

        public void EncodeImage(string inputPath, string outputPath, bool autoOrient, int width, int height, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            throw new NotImplementedException();
        }

        public void CreateImageCollage(ImageCollageOptions options)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { return "Null Image Encoder"; }
        }

        public bool SupportsImageCollageCreation
        {
            get { return false; }
        }

        public bool SupportsImageEncoding
        {
            get { return false; }
        }

        public void Dispose()
        {
        }
    }
}
