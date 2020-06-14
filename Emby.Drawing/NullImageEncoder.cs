using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;

namespace Emby.Drawing
{
    /// <summary>
    /// A fallback implementation of <see cref="IImageEncoder" />.
    /// </summary>
    public class NullImageEncoder : IImageEncoder
    {
        /// <inheritdoc />
        public IReadOnlyCollection<string> SupportedInputFormats
            => new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "png", "jpeg", "jpg" };

        /// <inheritdoc />
        public IReadOnlyCollection<ImageFormat> SupportedOutputFormats
        => new HashSet<ImageFormat>() { ImageFormat.Jpg, ImageFormat.Png };

        /// <inheritdoc />
        public string Name => "Null Image Encoder";

        /// <inheritdoc />
        public bool SupportsImageCollageCreation => false;

        /// <inheritdoc />
        public bool SupportsImageEncoding => false;

        /// <inheritdoc />
        public ImageDimensions GetImageSize(string path)
            => throw new NotImplementedException();

        /// <inheritdoc />
        public string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat selectedOutputFormat)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void CreateImageCollage(ImageCollageOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string GetImageBlurHash(int xComp, int yComp, string path)
        {
            throw new NotImplementedException();
        }
    }
}
