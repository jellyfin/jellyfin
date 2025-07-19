using MediaBrowser.Model.System;

namespace Jellyfin.Api.Models.SystemInfoDtos;

/// <summary>
/// Contains information about a specific folder.
/// </summary>
public record FolderStorageDto
{
    /// <summary>
    /// Gets the path of the folder in question.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the free space of the underlying storage device of the <see cref="Path"/>.
    /// </summary>
    public long FreeSpace { get; init; }

    /// <summary>
    /// Gets the used space of the underlying storage device of the <see cref="Path"/>.
    /// </summary>
    public long UsedSpace { get; init; }

    /// <summary>
    /// Gets the kind of storage device of the <see cref="Path"/>.
    /// </summary>
    public string? StorageType { get; init; }

    /// <summary>
    /// Gets the Device Identifier.
    /// </summary>
    public string? DeviceId { get; init; }

    internal static FolderStorageDto FromFolderStorageInfo(FolderStorageInfo model)
    {
        return new()
        {
            Path = model.Path,
            FreeSpace = model.FreeSpace,
            UsedSpace = model.UsedSpace,
            StorageType = model.StorageType,
            DeviceId = model.DeviceId
        };
    }
}
