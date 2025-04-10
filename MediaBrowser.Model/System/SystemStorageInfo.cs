#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Updates;

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
    public FolderStorageInfo ProgramDataDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the web UI resources path.
    /// </summary>
    /// <value>The web UI resources path.</value>
    public FolderStorageInfo WebDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the items by name path.
    /// </summary>
    /// <value>The items by name path.</value>
    public FolderStorageInfo ItemsByNameDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the cache path.
    /// </summary>
    /// <value>The cache path.</value>
    public FolderStorageInfo CacheDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the log path.
    /// </summary>
    /// <value>The log path.</value>
    public FolderStorageInfo LogDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata path.
    /// </summary>
    /// <value>The internal metadata path.</value>
    public FolderStorageInfo InternalMetadataDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the transcode path.
    /// </summary>
    /// <value>The transcode path.</value>
    public FolderStorageInfo TranscodingTempDirectoryInfo { get; set; }

    /// <summary>
    /// Gets or sets the storage informations of all libraries.
    /// </summary>
    public IReadOnlyCollection<LibraryStorageInfo> Libraries { get; set; }
}
