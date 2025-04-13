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
    public required FolderStorageDto ProgramDataFolder { get; set; }

    /// <summary>
    /// Gets or sets the web UI resources path.
    /// </summary>
    /// <value>The web UI resources path.</value>
    public required FolderStorageDto WebFolder { get; set; }

    /// <summary>
    /// Gets or sets the items by name path.
    /// </summary>
    /// <value>The items by name path.</value>
    public required FolderStorageDto ItemsByNameFolder { get; set; }

    /// <summary>
    /// Gets or sets the cache path.
    /// </summary>
    /// <value>The cache path.</value>
    public required FolderStorageDto CacheFolder { get; set; }

    /// <summary>
    /// Gets or sets the log path.
    /// </summary>
    /// <value>The log path.</value>
    public required FolderStorageDto LogFolder { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata path.
    /// </summary>
    /// <value>The internal metadata path.</value>
    public required FolderStorageDto InternalMetadataFolder { get; set; }

    /// <summary>
    /// Gets or sets the transcode path.
    /// </summary>
    /// <value>The transcode path.</value>
    public required FolderStorageDto TranscodingTempFolder { get; set; }

    /// <summary>
    /// Gets or sets the storage informations of all libraries.
    /// </summary>
    public required IReadOnlyCollection<LibraryStorageDto> Libraries { get; set; }

    internal static SystemStorageDto FromSystemStorageInfo(SystemStorageInfo model)
    {
        return new SystemStorageDto()
        {
            ProgramDataFolder = FolderStorageDto.FromFolderStorageInfo(model.ProgramDataFolder),
            WebFolder = FolderStorageDto.FromFolderStorageInfo(model.WebFolder),
            ItemsByNameFolder = FolderStorageDto.FromFolderStorageInfo(model.ItemsByNameFolder),
            CacheFolder = FolderStorageDto.FromFolderStorageInfo(model.CacheFolder),
            LogFolder = FolderStorageDto.FromFolderStorageInfo(model.LogFolder),
            InternalMetadataFolder = FolderStorageDto.FromFolderStorageInfo(model.InternalMetadataFolder),
            TranscodingTempFolder = FolderStorageDto.FromFolderStorageInfo(model.TranscodingTempFolder),
            Libraries = model.Libraries.Select(LibraryStorageDto.FromLibraryStorageModel).ToArray()
        };
    }
}
