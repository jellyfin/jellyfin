using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.System;

/// <summary>
/// Contains informations about a libraries storage informations.
/// </summary>
public class LibraryStorageInfo
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
    public required IReadOnlyCollection<FolderStorageInfo> Folders { get; set; }
}
