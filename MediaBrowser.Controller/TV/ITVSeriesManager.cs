using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using System.Collections.Generic;

namespace MediaBrowser.Controller.TV
{
    public interface ITVSeriesManager
    {
        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        QueryResult<BaseItem> GetNextUp(NextUpQuery query);

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="parentsFolders">The parents folders.</param>
        /// <returns>QueryResult&lt;BaseItem&gt;.</returns>
        QueryResult<BaseItem> GetNextUp(NextUpQuery request, List<Folder> parentsFolders);
    }
}
