using MediaBrowser.Model.Configuration;

namespace Jellyfin.Api.Models.LibraryStructureDto
{
    /// <summary>
    /// Library options dto.
    /// </summary>
    public class LibraryOptionsDto
    {
        /// <summary>
        /// Gets or sets library options.
        /// </summary>
        public LibraryOptions? LibraryOptions { get; set; }
    }
}