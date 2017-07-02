using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dto;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace MediaBrowser.Controller.Library
{
    public interface IMediaSourceProvider
    {
        /// <summary>
        /// Gets the media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetMediaSources(IHasMediaSources item, CancellationToken cancellationToken);

        /// <summary>
        /// Opens the media source.
        /// </summary>
        Task<Tuple<MediaSourceInfo,IDirectStreamProvider>> OpenMediaSource(string openToken, bool allowLiveStreamProbe, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the media source.
        /// </summary>
        /// <param name="liveStreamId">The live stream identifier.</param>
        /// <returns>Task.</returns>
        Task CloseMediaSource(string liveStreamId);
    }
}
