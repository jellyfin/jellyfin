using MediaBrowser.Controller.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Interface ILibrarySearchEngine
    /// </summary>
    public interface ILibrarySearchEngine
    {
        /// <summary>
        /// Sorts the specified items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        IEnumerable<BaseItem> Search(IEnumerable<BaseItem> items, string searchTerm);
    }
}
