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
    public required FolderStorageInfo ProgramDataFolder { get; set; }

    /// <summary>
    /// Gets or sets the web UI resources path.
    /// </summary>
    /// <value>The web UI resources path.</value>
    public required FolderStorageInfo WebFolder { get; set; }

    /// <summary>
    /// Gets or sets the items by name path.
    /// </summary>
    /// <value>The items by name path.</value>
    public required FolderStorageInfo ImageCacheFolder { get; set; }

    /// <summary>
    /// Gets or sets the cache path.
    /// </summary>
    /// <value>The cache path.</value>
    public required FolderStorageInfo CacheFolder { get; set; }

    /// <summary>
    /// Gets or sets the log path.
    /// </summary>
    /// <value>The log path.</value>
    public required FolderStorageInfo LogFolder { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata path.
    /// </summary>
    /// <value>The internal metadata path.</value>
    public required FolderStorageInfo InternalMetadataFolder { get; set; }

    /// <summary>
    /// Gets or sets the transcode path.
    /// </summary>
    /// <value>The transcode path.</value>
    public required FolderStorageInfo TranscodingTempFolder { get; set; }

    /// <summary>
    /// Gets or sets the storage informations of all libraries.
    /// </summary>
    public required IReadOnlyCollection<LibraryStorageInfo> Libraries { get; set; }
}
