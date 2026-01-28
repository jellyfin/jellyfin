using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Interface for search providers.
/// </summary>
public interface ISearchProvider
{
    /// <summary>
    /// Gets the name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the type of the provider.
    /// </summary>
    MetadataPluginType Type { get; }

    /// <summary>
    /// Gets the priority of the provider. Lower values execute first.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Searches for items matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ranked list of candidate item IDs with scores.</returns>
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        SearchProviderQuery query,
        CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether this provider can handle the given query.
    /// </summary>
    /// <param name="query">The search query to evaluate.</param>
    /// <returns>True if this provider can search for the query; otherwise, false.</returns>
    bool CanSearch(SearchProviderQuery query);
}
