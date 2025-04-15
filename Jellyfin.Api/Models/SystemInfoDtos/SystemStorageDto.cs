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
    /// Gets or sets the Storage information of the program data folder.
    /// </summary>
    public required FolderStorageDto ProgramDataFolder { get; set; }

    /// <summary>
    /// Gets or sets the Storage information of the web UI resources folder.
    /// </summary>
    public required FolderStorageDto WebFolder { get; set; }

    /// <summary>
    /// Gets or sets the Storage information of the folder where images are cached.
    /// </summary>
    public required FolderStorageDto ImageCacheFolder { get; set; }

    /// <summary>
    /// Gets or sets the Storage information of the cache folder.
    /// </summary>
    public required FolderStorageDto CacheFolder { get; set; }

    /// <summary>
    /// Gets or sets the Storage information of the folder where logfiles are saved to.
    /// </summary>
    public required FolderStorageDto LogFolder { get; set; }

    /// <summary>
    /// Gets or sets the Storage information of the folder where metadata is stored.
    /// </summary>
    public required FolderStorageDto InternalMetadataFolder { get; set; }

    /// <summary>
    /// Gets or sets the Storage information of the transcoding cache.
    /// </summary>
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
            ImageCacheFolder = FolderStorageDto.FromFolderStorageInfo(model.ImageCacheFolder),
            CacheFolder = FolderStorageDto.FromFolderStorageInfo(model.CacheFolder),
            LogFolder = FolderStorageDto.FromFolderStorageInfo(model.LogFolder),
            InternalMetadataFolder = FolderStorageDto.FromFolderStorageInfo(model.InternalMetadataFolder),
            TranscodingTempFolder = FolderStorageDto.FromFolderStorageInfo(model.TranscodingTempFolder),
            Libraries = model.Libraries.Select(LibraryStorageDto.FromLibraryStorageModel).ToArray()
        };
    }
}
