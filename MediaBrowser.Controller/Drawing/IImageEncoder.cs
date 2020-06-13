using System;
using System.Collections.Generic;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Controller.Drawing
{
    public interface IImageEncoder
    {
        /// <summary>
        /// Gets the supported input formats.
        /// </summary>
        /// <value>The supported input formats.</value>
        IReadOnlyCollection<string> SupportedInputFormats { get; }

        /// <summary>
        /// Gets the supported output formats.
        /// </summary>
        /// <value>The supported output formats.</value>
        IReadOnlyCollection<ImageFormat> SupportedOutputFormats { get; }

        /// <summary>
        /// Gets the display name for the encoder.
        /// </summary>
        /// <value>The display name.</value>
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

        /// <summary>
        /// Get the dimensions of an image from the filesystem.
        /// </summary>
        /// <param name="path">The filepath of the image.</param>
        /// <returns>The image dimensions.</returns>
        ImageDimensions GetImageSize(string path);

        /// <summary>
        /// Gets the blurhash of an image.
        /// </summary>
        /// <param name="xComp">Amount of X components of DCT to take.</param>
        /// <param name="yComp">Amount of Y components of DCT to take.</param>
        /// <param name="path">The filepath of the image.</param>
        /// <returns>The blurhash.</returns>
        string GetImageBlurHash(int xComp, int yComp, string path);

        /// <summary>
        /// Encode an image.
        /// </summary>
        string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat outputFormat);

        /// <summary>
        /// Create an image collage.
        /// </summary>
        /// <param name="options">The options to use when creating the collage.</param>
        void CreateImageCollage(ImageCollageOptions options);
    }
}
