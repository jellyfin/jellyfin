using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.Dto;

namespace Jellyfin.Controller.Library
{
    public interface IMediaSourceProvider
    {
        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetMediaSources(BaseItem item, CancellationToken cancellationToken);

        /// <summary>
        /// Opens the media source.
        /// </summary>
        Task<ILiveStream> OpenMediaSource(string openToken, List<ILiveStream> currentLiveStreams, CancellationToken cancellationToken);
    }
}
