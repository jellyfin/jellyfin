namespace MediaBrowser.Controller.Drawing
{
    /// <summary>
    /// Interface for an image generator.
    /// </summary>
    public interface IImageGenerator
    {
        /// <summary>
        /// Gets the supported generated images of the image generator.
        /// </summary>
        /// <returns>The supported images.</returns>
        GeneratedImages[] GetSupportedImages();

        /// <summary>
        /// Generates a splashscreen.
        /// </summary>
        /// <param name="outputPath">The path where the splashscreen should be saved.</param>
        void GenerateSplashscreen(string outputPath);
    }
}
