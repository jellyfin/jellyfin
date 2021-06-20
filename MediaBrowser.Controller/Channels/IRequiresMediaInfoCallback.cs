using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// The channel requires a media info callback.
    /// </summary>
    public interface IRequiresMediaInfoCallback
    {
        /// <summary>
        /// Gets the channel item media information.
        /// </summary>
        /// <param name="id">The channel item id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The enumerable of media source info.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken);
    }
}
