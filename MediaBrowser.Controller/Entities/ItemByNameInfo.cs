#pragma warning disable CA2227 // Collection properties should be read only

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Entities;

/// <summary>
/// A genre or studio a metadata provider named, with the ids it knows it by.
/// </summary>
public sealed class ItemByNameInfo : IHasProviderIds
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemByNameInfo"/> class.
    /// </summary>
    /// <param name="name">The name the provider gave.</param>
    public ItemByNameInfo(string name)
    {
        Name = name;
        ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the ids the provider knows this genre or studio by.
    /// </summary>
    public Dictionary<string, string> ProviderIds { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name;
    }
}
