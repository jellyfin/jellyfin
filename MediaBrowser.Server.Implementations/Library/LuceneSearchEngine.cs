using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library
{
    /// <summary>
    /// Class LuceneSearchEngine
    /// http://www.codeproject.com/Articles/320219/Lucene-Net-ultra-fast-search-for-MVC-or-WebForms
    /// </summary>
    public class LuceneSearchEngine : ILibrarySearchEngine
    {
        /// <summary>
        /// Searches items and returns them in order of relevance.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>IEnumerable{BaseItem}.</returns>
        /// <exception cref="System.ArgumentNullException">searchTerm</exception>
        public IEnumerable<BaseItem> Search(IEnumerable<BaseItem> items, string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                throw new ArgumentNullException("searchTerm");
            }

            return items.Where(i => i.Name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) != -1);
        }
    }
}
