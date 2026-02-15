using MediaBrowser.Model.Configuration;

namespace Jellyfin.Api.Models.LibraryStructureDto;

/// <summary>
/// Add virtual folder dto.
/// </summary>
public class AddVirtualFolderDto
{
    /// <summary>
    /// Gets or sets library options.
    /// </summary>
    public LibraryOptions? LibraryOptions { get; set; }
}
