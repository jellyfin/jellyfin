#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.ComponentModel;
using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.System;

/// <summary>
/// Contains informations about a libraries storage informations.
/// </summary>
public class LibraryStorageInfo
{
    /// <summary>
    /// Gets or sets the Library Id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the library.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the storage informations about the folders used in a library.
    /// </summary>
    public IReadOnlyCollection<FolderStorageInfo> Folders { get; set; }
}
