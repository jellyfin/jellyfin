using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace MediaBrowser.Controller.Library
{
    public interface IMediaSourceManager
    {
        /// <summary>
        /// Adds the parts.
        /// </summary>
        /// <param name="providers">The providers.</param>
        void AddParts(IEnumerable<IMediaSourceProvider> providers);

        /// <summary>
        /// Gets the media streams.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>IEnumerable&lt;MediaStream&gt;.</returns>
        IEnumerable<MediaStream> GetMediaStreams(Guid itemId);
        /// <summary>
        /// Gets the media streams.
        /// </summary>
        /// <param name="mediaSourceId">The media source identifier.</param>
        /// <returns>IEnumerable&lt;MediaStream&gt;.</returns>
        IEnumerable<MediaStream> GetMediaStreams(string mediaSourceId);
        /// <summary>
        /// Gets the media streams.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable&lt;MediaStream&gt;.</returns>
        IEnumerable<MediaStream> GetMediaStreams(MediaStreamQuery query);

        /// <summary>
        /// Gets the playack media sources.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="userId">The user identifier.</param>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <param name="supportedLiveMediaTypes">The supported live media types.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable&lt;MediaSourceInfo&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetPlayackMediaSources(string id, string userId, bool enablePathSubstitution, string[] supportedLiveMediaTypes, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the static media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable&lt;MediaSourceInfo&gt;.</returns>
        IEnumerable<MediaSourceInfo> GetStaticMediaSources(IHasMediaSources item, bool enablePathSubstitution, User user = null);

        /// <summary>
        /// Gets the static media source.
        /// </summary>
        /// <returns>MediaSourceInfo.</returns>
        Task<MediaSourceInfo> GetMediaSource(IHasMediaSources item, string mediaSourceId, string liveStreamId, bool enablePathSubstitution, CancellationToken cancellationToken);

        /// <summary>
        /// Opens the media source.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="enableAutoClose">if set to <c>true</c> [enable automatic close].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;MediaSourceInfo&gt;.</returns>
        Task<LiveStreamResponse> OpenLiveStream(LiveStreamRequest request, bool enableAutoClose, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;MediaSourceInfo&gt;.</returns>
        Task<MediaSourceInfo> GetLiveStream(string id, CancellationToken cancellationToken);

        Task<Tuple<MediaSourceInfo, IDirectStreamProvider>> GetLiveStreamWithDirectStreamProvider(string id, CancellationToken cancellationToken);
        
        /// <summary>
        /// Pings the media source.
        /// </summary>
        /// <param name="id">The live stream identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task PingLiveStream(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Closes the media source.
        /// </summary>
        /// <param name="id">The live stream identifier.</param>
        /// <returns>Task.</returns>
        Task CloseLiveStream(string id);
    }

    public interface IDirectStreamProvider
    {
        Task CopyToAsync(Stream stream, CancellationToken cancellationToken);
    }
}
