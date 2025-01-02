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
    /// Gets all serialisation target types for music related kinds.
    /// </summary>
    IReadOnlyList<string> MusicGenreTypes { get; }

    /// <summary>
    /// Gets mapping for all BaseItemKinds and their expected serialization target.
    /// </summary>
    IReadOnlyDictionary<BaseItemKind, string> BaseItemKindNames { get; }
}
