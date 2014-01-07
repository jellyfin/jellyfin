using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Search;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
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
        Task<QueryResult<SearchHintInfo>> GetSearchHints(SearchQuery query);
    }
}
