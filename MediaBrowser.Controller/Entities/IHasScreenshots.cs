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
    }
}
