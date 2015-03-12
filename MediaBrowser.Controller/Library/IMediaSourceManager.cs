using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IEnumerable&lt;MediaSourceInfo&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetPlayackMediaSources(string id, string userId, bool enablePathSubstitution, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the playack media sources.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;IEnumerable&lt;MediaSourceInfo&gt;&gt;.</returns>
        Task<IEnumerable<MediaSourceInfo>> GetPlayackMediaSources(string id, bool enablePathSubstitution, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the static media sources.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <param name="user">The user.</param>
        /// <returns>IEnumerable&lt;MediaSourceInfo&gt;.</returns>
        IEnumerable<MediaSourceInfo> GetStaticMediaSources(IHasMediaSources item, bool enablePathSubstitution, User user);

        /// <summary>
        /// Gets the static media source.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="mediaSourceId">The media source identifier.</param>
        /// <param name="enablePathSubstitution">if set to <c>true</c> [enable path substitution].</param>
        /// <returns>MediaSourceInfo.</returns>
        MediaSourceInfo GetStaticMediaSource(IHasMediaSources item, string mediaSourceId, bool enablePathSubstitution);
    }
}
