using System.Collections.Generic;

namespace MediaBrowser.Model.System;

/// <summary>
/// Contains informations about the systems storage.
/// </summary>
public class SystemStorageInfo
{
    /// <summary>
    /// Gets or sets the program data path.
    /// </summary>
    /// <value>The program data path.</value>
    public required FolderStorageInfo ProgramDataStorage { get; set; }

    /// <summary>
    /// Gets or sets the web UI resources path.
    /// </summary>
    /// <value>The web UI resources path.</value>
    public required FolderStorageInfo WebStorage { get; set; }

    /// <summary>
    /// Gets or sets the items by name path.
    /// </summary>
    /// <value>The items by name path.</value>
    public required FolderStorageInfo ItemsByNameStorage { get; set; }

    /// <summary>
    /// Gets or sets the cache path.
    /// </summary>
    /// <value>The cache path.</value>
    public required FolderStorageInfo CacheStorage { get; set; }

    /// <summary>
    /// Gets or sets the log path.
    /// </summary>
    /// <value>The log path.</value>
    public required FolderStorageInfo LogStorage { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata path.
    /// </summary>
    /// <value>The internal metadata path.</value>
    public required FolderStorageInfo InternalMetadataStorage { get; set; }

    /// <summary>
    /// Gets or sets the transcode path.
    /// </summary>
    /// <value>The transcode path.</value>
    public required FolderStorageInfo TranscodingTempStorage { get; set; }

    /// <summary>
    /// Gets or sets the storage informations of all libraries.
    /// </summary>
    public required IReadOnlyCollection<LibraryStorageInfo> LibrariesStorage { get; set; }
}
