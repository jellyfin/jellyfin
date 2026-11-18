using System.Collections.Generic;
using System.Threading;

namespace MediaBrowser.Controller.Library;

/// <summary>
/// Interface for external search providers that offer enhanced search capabilities.
/// </summary>
public interface IExternalSearchProvider : ISearchProvider
{
    /// <summary>
    /// Searches for items matching the query.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of search results with relevance scores.</returns>
    new IAsyncEnumerable<SearchResult> SearchAsync(
        SearchProviderQuery query,
        CancellationToken cancellationToken);
}
