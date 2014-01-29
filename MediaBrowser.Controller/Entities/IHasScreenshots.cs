using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasScreenshots
    /// </summary>
    public interface IHasScreenshots
    {
        /// <summary>
        /// Gets or sets the screenshot image paths.
        /// </summary>
        /// <value>The screenshot image paths.</value>
        List<string> ScreenshotImagePaths { get; set; }

        /// <summary>
        /// Determines whether [contains image with source URL] [the specified URL].
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns><c>true</c> if [contains image with source URL] [the specified URL]; otherwise, <c>false</c>.</returns>
        bool ContainsImageWithSourceUrl(string url);
    }
}
