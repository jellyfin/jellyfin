using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.System;

namespace Jellyfin.Api.Models.SystemInfoDtos;

/// <summary>
/// Contains informations about a libraries storage informations.
/// </summary>
public record LibraryStorageDto
{
      /// <summary>
    /// Gets or sets the Library Id.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the library.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the storage informations about the folders used in a library.
    /// </summary>
    public required IReadOnlyCollection<FolderStorageDto> Folders { get; set; }

    internal static LibraryStorageDto FromLibraryStorageModel(LibraryStorageInfo model)
    {
        return new()
        {
            Id = model.Id,
            Name = model.Name,
            Folders = model.Folders.Select(FolderStorageDto.FromFolderStorageInfo).ToArray()
        };
    }
}
