using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    public interface IMediaSourceManager
    {
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
    }
}
