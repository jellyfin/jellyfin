#nullable enable

namespace Jellyfin.Api.Models.Branding
{
    /// <summary>
    /// The branding DTO.
    /// </summary>
    public class BrandingDto
    {
        /// <summary>
        /// Gets or sets the login disclaimer. Defaults to an empty string.
        /// </summary>
        public string LoginDisclaimer { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the custom CSS. Defaults to an empty string.
        /// </summary>
        public string CustomCss { get; set; } = string.Empty;
    }
}
