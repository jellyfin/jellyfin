namespace MediaBrowser.Controller.Drawing
{
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
        /// <param name="generationOptions">The options used to generate the splashscreen.</param>
        void GenerateSplashscreen(SplashscreenOptions generationOptions);
    }
}
