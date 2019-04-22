using Jellyfin.Controller.Dto;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.Querying;

namespace Jellyfin.Controller.TV
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
