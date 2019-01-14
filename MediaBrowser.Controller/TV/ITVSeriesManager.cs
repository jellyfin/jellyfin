using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using MediaBrowser.Controller.Dto;

namespace MediaBrowser.Controller.TV
{
    public interface ITVSeriesManager
    {
        /// <summary>
        /// Gets the next up.
        /// </summary>
        QueryResult<BaseItem> GetNextUp(NextUpQuery query, DtoOptions options);

        /// <summary>
        /// Gets the next up.
        /// </summary>
        QueryResult<BaseItem> GetNextUp(NextUpQuery request, BaseItem[] parentsFolders, DtoOptions options);
    }
}
