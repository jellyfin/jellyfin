#pragma warning disable CS1591

using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.TV
{
    /// <summary>
    /// The TV Series manager.
    /// </summary>
    public interface ITVSeriesManager
    {
        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="query">The next up query.</param>
        /// <param name="options">The dto options.</param>
        /// <returns>The next up items.</returns>
        QueryResult<BaseItem> GetNextUp(NextUpQuery query, DtoOptions options);

        /// <summary>
        /// Gets the next up.
        /// </summary>
        /// <param name="request">The next up request.</param>
        /// <param name="parentsFolders">The list of parent folders.</param>
        /// <param name="options">The dto options.</param>
        /// <returns>The next up items.</returns>
        QueryResult<BaseItem> GetNextUp(NextUpQuery request, BaseItem[] parentsFolders, DtoOptions options);
    }
}
