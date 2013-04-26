using MediaBrowser.Controller.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface ILibrarySearchEngine
    /// </summary>
    public interface ILibrarySearchEngine
    {
        /// <summary>
        /// Searches items and returns them in order of relevance.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        IEnumerable<BaseItem> Search(IEnumerable<BaseItem> items, string searchTerm);

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <param name="inputItems">The input items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>Task{IEnumerable{BaseItem}}.</returns>
        Task<IEnumerable<BaseItem>> GetSearchHints(IEnumerable<BaseItem> inputItems, string searchTerm);
    }
}
