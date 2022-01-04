using System.Collections.Generic;

namespace MediaBrowser.Controller.Drawing;

/// <summary>
/// Interface for an image generator.
/// </summary>
public interface IImageGenerator
{
    /// <summary>
    /// Gets the supported generated images of the image generator.
    /// </summary>
    /// <returns>The supported generated image types.</returns>
    IReadOnlyList<GeneratedImageType> GetSupportedImages();

    /// <summary>
    /// Generates a splashscreen.
    /// </summary>
    /// <param name="imageTypeType">The image to generate.</param>
    /// <param name="outputPath">The path where the splashscreen should be saved.</param>
    void Generate(GeneratedImageType imageTypeType, string outputPath);
}
