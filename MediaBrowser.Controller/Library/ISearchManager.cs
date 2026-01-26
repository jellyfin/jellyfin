using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Orchestrates search operations across registered search providers.
/// </summary>
public interface ISearchManager
{
    /// <summary>
    /// Searches for items and returns hints suitable for autocomplete/typeahead UI.
    /// Results are ordered by relevance score from search providers.
    /// </summary>
    /// <param name="query">The search query including filters and pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated search hints with item metadata for display.</returns>
    Task<QueryResult<SearchHintInfo>> GetSearchHintsAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets search results with their items pre-fetched from search providers.
    /// </summary>
    /// <param name="query">The search provider query with type/media filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Search results containing item IDs, relevance scores, and optionally pre-fetched BaseItem data.</returns>
    Task<IReadOnlyList<SearchResult>> GetSearchResultsWithItemsAsync(
        SearchProviderQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers search providers discovered through dependency injection.
    /// Called during application startup.
    /// </summary>
    /// <param name="providers">The search providers to register.</param>
    void AddParts(IEnumerable<ISearchProvider> providers);

    /// <summary>
    /// Gets all registered search providers ordered by priority.
    /// </summary>
    /// <returns>The list of search providers including the SQL fallback provider.</returns>
    IReadOnlyList<ISearchProvider> GetProviders();
}
