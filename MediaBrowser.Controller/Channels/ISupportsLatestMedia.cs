using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// Interface for channels that support retrieving the latest media.
    /// </summary>
    public interface ISupportsLatestMedia
    {
        /// <summary>
        /// Gets the latest media.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The latest media.</returns>
        Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken);
    }
}
