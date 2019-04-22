using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Model.Dto;

namespace Jellyfin.Controller.Channels
{
    public interface IRequiresMediaInfoCallback
    {
        /// <summary>
        /// Gets the channel item media information.
        /// </summary>
        Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken);
    }
}
