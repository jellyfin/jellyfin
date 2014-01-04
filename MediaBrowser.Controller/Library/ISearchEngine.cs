using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
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
        /// <param name="inputItems">The input items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>Task{IEnumerable{SearchHintInfo}}.</returns>
        Task<IEnumerable<SearchHintInfo>> GetSearchHints(IEnumerable<BaseItem> inputItems, string searchTerm);
    }
}
