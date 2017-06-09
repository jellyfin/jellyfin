using System;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Controller.Drawing
{
    public interface IImageEncoder : IDisposable
    {
        /// <summary>
        /// Gets the supported input formats.
        /// </summary>
        /// <value>The supported input formats.</value>
        string[] SupportedInputFormats { get; }
        /// <summary>
        /// Gets the supported output formats.
        /// </summary>
        /// <value>The supported output formats.</value>
        ImageFormat[] SupportedOutputFormats { get; }

        /// <summary>
        /// Encodes the image.
        /// </summary>
        string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat outputFormat);

        /// <summary>
        /// Creates the image collage.
        /// </summary>
        /// <param name="options">The options.</param>
        void CreateImageCollage(ImageCollageOptions options);
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether [supports image collage creation].
        /// </summary>
        /// <value><c>true</c> if [supports image collage creation]; otherwise, <c>false</c>.</value>
        bool SupportsImageCollageCreation { get; }

        /// <summary>
        /// Gets a value indicating whether [supports image encoding].
        /// </summary>
        /// <value><c>true</c> if [supports image encoding]; otherwise, <c>false</c>.</value>
        bool SupportsImageEncoding { get; }

        ImageSize GetImageSize(string path);
    }
}
