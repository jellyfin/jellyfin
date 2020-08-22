#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Channels
{
    public interface IRequiresMediaInfoCallback
    {
        /// <summary>
        /// Gets the channel item media information.
        /// </summary>
        Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken);
    }
}
