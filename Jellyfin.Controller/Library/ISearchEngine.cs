using Jellyfin.Model.Querying;
using Jellyfin.Model.Search;

namespace Jellyfin.Controller.Library
{
    /// <summary>
    /// Interface ILibrarySearchEngine
    /// </summary>
    public interface ISearchEngine
    {
        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>Task{IEnumerable{SearchHintInfo}}.</returns>
        QueryResult<SearchHintInfo> GetSearchHints(SearchQuery query);
    }
}
