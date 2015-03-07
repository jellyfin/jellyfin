using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    public class LiveMediaInfoResult
    {
        /// <summary>
        /// Gets or sets the media sources.
        /// </summary>
        /// <value>The media sources.</value>
        public List<MediaSourceInfo> MediaSources { get; set; }

        /// <summary>
        /// Gets or sets the live stream identifier.
        /// </summary>
        /// <value>The live stream identifier.</value>
        public string LiveStreamId { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        /// <value>The error code.</value>
        public PlaybackErrorCode? ErrorCode { get; set; }

        public LiveMediaInfoResult()
        {
            MediaSources = new List<MediaSourceInfo>();
        }
    }
}
