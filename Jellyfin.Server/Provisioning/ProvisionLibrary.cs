using System.Collections.Generic;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Server.Provisioning;

/// <summary>
/// A library described by a <see cref="ProvisionManifest"/>.
/// </summary>
public sealed class ProvisionLibrary
{
    /// <summary>
    /// Gets or sets the library name as shown to clients.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the paths the library is built from.
    /// </summary>
    public required IReadOnlyList<string> Paths { get; set; }

    /// <summary>
    /// Gets or sets the type of content the library holds. A null value creates a library with no
    /// declared type, which the web UI presents as "Other".
    /// </summary>
    public CollectionTypeOptions? CollectionType { get; set; }

    /// <summary>
    /// Gets or sets the library options. Server defaults apply when omitted.
    /// </summary>
    public LibraryOptions? LibraryOptions { get; set; }
}
