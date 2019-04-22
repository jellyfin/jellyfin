using System;
using System.Collections.Generic;
using Jellyfin.Controller.Drawing;
using Jellyfin.Model.Drawing;

namespace Jellyfin.Drawing
{
    public class NullImageEncoder : IImageEncoder
    {
        public IReadOnlyCollection<string> SupportedInputFormats
            => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "png", "jpeg", "jpg" };

        public IReadOnlyCollection<ImageFormat> SupportedOutputFormats
        => new HashSet<ImageFormat>() { ImageFormat.Jpg, ImageFormat.Png };

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
