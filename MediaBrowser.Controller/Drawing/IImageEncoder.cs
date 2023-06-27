#pragma warning disable CS1591

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
        /// <param name="inputPath">Input path of image.</param>
        /// <param name="dateModified">Date modified.</param>
        /// <param name="outputPath">Output path of image.</param>
        /// <param name="autoOrient">Auto-orient image.</param>
        /// <param name="orientation">Desired orientation of image.</param>
        /// <param name="quality">Quality of encoded image.</param>
        /// <param name="options">Image processing options.</param>
        /// <param name="outputFormat">Image format of output.</param>
        /// <returns>Path of encoded image.</returns>
        string EncodeImage(string inputPath, DateTime dateModified, string outputPath, bool autoOrient, ImageOrientation? orientation, int quality, ImageProcessingOptions options, ImageFormat outputFormat);

        /// <summary>
        /// Create an image collage.
        /// </summary>
        /// <param name="options">The options to use when creating the collage.</param>
        /// <param name="libraryName">Optional. </param>
        void CreateImageCollage(ImageCollageOptions options, string? libraryName);

        /// <summary>
        /// Creates a new splashscreen image.
        /// </summary>
        /// <param name="posters">The list of poster paths.</param>
        /// <param name="backdrops">The list of backdrop paths.</param>
        void CreateSplashscreen(IReadOnlyList<string> posters, IReadOnlyList<string> backdrops);

        /// <summary>
        /// Creates a new trickplay tile image.
        /// </summary>
        /// <param name="options">The options to use when creating the image. Width and Height are a quantity of thumbnails in this case, not pixels.</param>
        /// <param name="quality">The image encode quality.</param>
        /// <param name="imgWidth">The width of a single trickplay thumbnail.</param>
        /// <param name="imgHeight">Optional height of a single trickplay thumbnail, if it is known.</param>
        /// <returns>Height of single decoded trickplay thumbnail.</returns>
        int CreateTrickplayTile(ImageCollageOptions options, int quality, int imgWidth, int? imgHeight);
    }
}
