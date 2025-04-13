using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.System;

namespace Jellyfin.Api.Models.SystemInfoDtos;

/// <summary>
/// Contains informations about the systems storage.
/// </summary>
public record SystemStorageDto
{
    /// <summary>
    /// Gets or sets the program data path.
    /// </summary>
    /// <value>The program data path.</value>
    public required FolderStorageDto ProgramDataStorage { get; set; }

    /// <summary>
    /// Gets or sets the web UI resources path.
    /// </summary>
    /// <value>The web UI resources path.</value>
    public required FolderStorageDto WebStorage { get; set; }

    /// <summary>
    /// Gets or sets the items by name path.
    /// </summary>
    /// <value>The items by name path.</value>
    public required FolderStorageDto ItemsByNameStorage { get; set; }

    /// <summary>
    /// Gets or sets the cache path.
    /// </summary>
    /// <value>The cache path.</value>
    public required FolderStorageDto CacheStorage { get; set; }

    /// <summary>
    /// Gets or sets the log path.
    /// </summary>
    /// <value>The log path.</value>
    public required FolderStorageDto LogStorage { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata path.
    /// </summary>
    /// <value>The internal metadata path.</value>
    public required FolderStorageDto InternalMetadataStorage { get; set; }

    /// <summary>
    /// Gets or sets the transcode path.
    /// </summary>
    /// <value>The transcode path.</value>
    public required FolderStorageDto TranscodingTempStorage { get; set; }

    /// <summary>
    /// Gets or sets the storage informations of all libraries.
    /// </summary>
    public required IReadOnlyCollection<LibraryStorageDto> LibrariesStorage { get; set; }

    internal static SystemStorageDto FromSystemStorageInfo(SystemStorageInfo model)
    {
        return new SystemStorageDto()
        {
            ProgramDataStorage = FolderStorageDto.FromFolderStorageInfo(model.ProgramDataStorage),
            WebStorage = FolderStorageDto.FromFolderStorageInfo(model.WebStorage),
            ItemsByNameStorage = FolderStorageDto.FromFolderStorageInfo(model.ItemsByNameStorage),
            CacheStorage = FolderStorageDto.FromFolderStorageInfo(model.CacheStorage),
            LogStorage = FolderStorageDto.FromFolderStorageInfo(model.LogStorage),
            InternalMetadataStorage = FolderStorageDto.FromFolderStorageInfo(model.InternalMetadataStorage),
            TranscodingTempStorage = FolderStorageDto.FromFolderStorageInfo(model.TranscodingTempStorage),
            LibrariesStorage = model.LibrariesStorage.Select(LibraryStorageDto.FromLibraryStorageModel).ToArray()
        };
    }
}
