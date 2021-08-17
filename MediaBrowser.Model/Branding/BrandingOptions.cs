using System.Xml.Serialization;

#pragma warning disable CS1591

namespace MediaBrowser.Model.Branding
{
    public class BrandingOptions
    {
        /// <summary>
        /// Gets or sets the login disclaimer.
        /// </summary>
        /// <value>The login disclaimer.</value>
        public string? LoginDisclaimer { get; set; }

        /// <summary>
        /// Gets or sets the custom CSS.
        /// </summary>
        /// <value>The custom CSS.</value>
        public string? CustomCss { get; set; }

        /// <summary>
        /// Gets or sets the splashscreen location on disk.
        /// </summary>
        /// <value>The location of the user splashscreen.</value>
        public string? SplashscreenLocation { get; set; }
    }
}
