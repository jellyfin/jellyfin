using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Persistence;

/// <summary>
/// Provides static lookup data for <see cref="ItemFields"/> and <see cref="BaseItemKind"/> for the domain.
/// </summary>
public interface IItemTypeLookup
{
    /// <summary>
    /// Gets all values of the ItemFields type.
    /// </summary>
    public IReadOnlyList<ItemFields> AllItemFields { get; }

    /// <summary>
    /// Gets all BaseItemKinds that are considered Programs.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ProgramTypes { get; }

    /// <summary>
    /// Gets all BaseItemKinds that should be excluded from parent lookup.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ProgramExcludeParentTypes { get; }

    /// <summary>
    /// Gets all BaseItemKinds that are considered to be provided by services.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ServiceTypes { get; }

    /// <summary>
    /// Gets all BaseItemKinds that have a StartDate.
    /// </summary>
    public IReadOnlyList<BaseItemKind> StartDateTypes { get; }

    /// <summary>
    /// Gets all BaseItemKinds that are considered Series.
    /// </summary>
    public IReadOnlyList<BaseItemKind> SeriesTypes { get; }

    /// <summary>
    /// Gets all BaseItemKinds that are not to be evaluated for Artists.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ArtistExcludeParentTypes { get; }

    /// <summary>
    /// Gets all BaseItemKinds that are considered Artists.
    /// </summary>
    public IReadOnlyList<BaseItemKind> ArtistsTypes { get; }

    /// <summary>
    /// Gets mapping for all BaseItemKinds and their expected serialization target.
    /// </summary>
    public IDictionary<BaseItemKind, string?> BaseItemKindNames { get; }
}
