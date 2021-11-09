#nullable disable

#pragma warning disable CA1002, CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

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
        List<MediaStream> GetMediaStreams(Guid itemId);

        /// <summary>
        /// Gets the media streams.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable&lt;MediaStream&gt;.</returns>
        List<MediaStream> GetMediaStreams(MediaStreamQuery query);

        /// <summary>
        /// Gets the media attachments.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>IEnumerable&lt;MediaAttachment&gt;.</returns>
        List<MediaAttachment> GetMediaAttachments(Guid itemId);

        /// <summary>
        /// Gets the media attachments.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>IEnumerable&lt;MediaAttachment&gt;.</returns>
        List<MediaAttachment> GetMediaAttachments(MediaAttachmentQuery query);

        /// <summary>
        /// Gets the playack media sources.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="user">User to use for operation.</param>
        /// <param name="allowMediaProbe">Option to allow media probe.</param>
        /// <param name="enablePathSubstitution">Option to enable path substitution.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>List of media sources wrapped in an awaitable task.</returns>
        Task<List<MediaSourceInfo>> GetPlaybackMediaSources(BaseItem item, User user, bool allowMediaProbe, bool enablePathSubstitution, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the static media sources.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="enablePathSubstitution">Option to enable path substitution.</param>
        /// <param name="user">User to use for operation.</param>
        /// <returns>List of media sources.</returns>
        List<MediaSourceInfo> GetStaticMediaSources(BaseItem item, bool enablePathSubstitution, User user = null);

        /// <summary>
        /// Gets the static media source.
        /// </summary>
        /// <param name="item">Item to use.</param>
        /// <param name="mediaSourceId">Media source to get.</param>
        /// <param name="liveStreamId">Live stream to use.</param>
        /// <param name="enablePathSubstitution">Option to enable path substitution.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>The static media source wrapped in an awaitable task.</returns>
        Task<MediaSourceInfo> GetMediaSource(BaseItem item, string mediaSourceId, string liveStreamId, bool enablePathSubstitution, CancellationToken cancellationToken);

        /// <summary>
        /// Opens the media source.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;MediaSourceInfo&gt;.</returns>
        Task<LiveStreamResponse> OpenLiveStream(LiveStreamRequest request, CancellationToken cancellationToken);

        Task<Tuple<LiveStreamResponse, IDirectStreamProvider>> OpenLiveStreamInternal(LiveStreamRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live stream.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;MediaSourceInfo&gt;.</returns>
        Task<MediaSourceInfo> GetLiveStream(string id, CancellationToken cancellationToken);

        Task<Tuple<MediaSourceInfo, IDirectStreamProvider>> GetLiveStreamWithDirectStreamProvider(string id, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the live stream info.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>An instance of <see cref="ILiveStream"/>.</returns>
        public ILiveStream GetLiveStreamInfo(string id);

        /// <summary>
        /// Gets the live stream info using the stream's unique id.
        /// </summary>
        /// <param name="uniqueId">The unique identifier.</param>
        /// <returns>An instance of <see cref="ILiveStream"/>.</returns>
        public ILiveStream GetLiveStreamInfoByUniqueId(string uniqueId);

        /// <summary>
        /// Closes the media source.
        /// </summary>
        /// <param name="id">The live stream identifier.</param>
        /// <returns>Task.</returns>
        Task CloseLiveStream(string id);

        Task<MediaSourceInfo> GetLiveStreamMediaInfo(string id, CancellationToken cancellationToken);

        bool SupportsDirectStream(string path, MediaProtocol protocol);

        MediaProtocol GetPathProtocol(string path);

        void SetDefaultAudioAndSubtitleStreamIndexes(BaseItem item, MediaSourceInfo source, User user);

        Task AddMediaInfoWithProbe(MediaSourceInfo mediaSource, bool isAudio, string cacheKey, bool addProbeDelay, bool isLiveStream, CancellationToken cancellationToken);
    }
}
