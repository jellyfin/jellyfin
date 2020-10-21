#pragma warning disable CS1591

using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.TV
{
    public interface ITVSeriesManager
    {
        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="query">Query.</param>
        /// <param name="options">Dto options.</param>
        /// <returns>Query results.</returns>
        QueryResult<BaseItem> GetNextUp(NextUpQuery query, DtoOptions options);

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="request">Query.</param>
        /// <param name="parentsFolders">Parent folders.</param>
        /// <param name="options">Dto options.</param>
        /// <returns>Query results.</returns>
        QueryResult<BaseItem> GetNextUp(NextUpQuery request, BaseItem[] parentsFolders, DtoOptions options);
    }
}
