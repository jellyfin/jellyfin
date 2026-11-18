using System;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Query object for search providers.
/// </summary>
public class SearchProviderQuery
{
    /// <summary>
    /// Gets the search term.
    /// </summary>
    public required string SearchTerm { get; init; }

    /// <summary>
    /// Gets the user ID for user-specific searches.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the item types to include in the search. An empty array means every type is eligible.
    /// When this is non-empty it is the authoritative type filter and <see cref="ExcludeItemTypes"/>
    /// does not apply; excludes only take effect when no include types were requested.
    /// </summary>
    public BaseItemKind[] IncludeItemTypes { get; init; } = [];

    /// <summary>
    /// Gets the item types to exclude from the search.
    /// </summary>
    public BaseItemKind[] ExcludeItemTypes { get; init; } = [];

    /// <summary>
    /// Gets the media types to include in the search. This is an additional constraint rather than
    /// an alternative one: a provider must return only items that match both the requested media
    /// types and the requested item types, not the union of the two.
    /// </summary>
    public MediaType[] MediaTypes { get; init; } = [];

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Gets the parent ID to scope the search. This scopes to the whole subtree, not just direct
    /// children - callers routinely pass a library folder id and expect items nested arbitrarily
    /// deep beneath it (an episode under a season under a series) to match.
    /// </summary>
    public Guid? ParentId { get; init; }
}
