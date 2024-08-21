using System.Collections.Generic;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.SearchExtension;

/// <summary>
/// Provides methods for obtaining a set of items though an externally provided method.
/// </summary>
public interface IBaseItemSearchProvider
{
    /// <summary>
    /// Searches for items matching the set Criteria.
    /// </summary>
    /// <param name="source">The source or null this request should be based on.</param>
    /// <param name="query">The arguments to filter the selection of.</param>
    /// <returns>A async list of search results.</returns>
    IAsyncEnumerable<BaseItem> Search(BaseItem source, InternalItemsQuery query);
}
