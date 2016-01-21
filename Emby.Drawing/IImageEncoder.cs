using MediaBrowser.Controller.Drawing;
using MediaBrowser.Model.Drawing;
using System;

namespace Emby.Drawing
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
        /// Crops the white space.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="outputPath">The output path.</param>
        void CropWhiteSpace(string inputPath, string outputPath);
        /// <summary>
        /// Encodes the image.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <param name="outputPath">The output path.</param>
        /// <param name="autoOrient">if set to <c>true</c> [automatic orient].</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="quality">The quality.</param>
        /// <param name="options">The options.</param>
        /// <param name="outputFormat">The output format.</param>
        void EncodeImage(string inputPath, string outputPath, bool autoOrient, int width, int height, int quality, ImageProcessingOptions options, ImageFormat outputFormat);

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
    }
}
