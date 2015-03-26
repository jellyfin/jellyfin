using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo
{
    public class PlaybackInfoResponse
    {
        /// <summary>
        /// Gets or sets the media sources.
        /// </summary>
        /// <value>The media sources.</value>
        public List<MediaSourceInfo> MediaSources { get; set; }

        /// <summary>
        /// Gets or sets the stream identifier.
        /// </summary>
        /// <value>The stream identifier.</value>
        public string StreamId { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        /// <value>The error code.</value>
        public PlaybackErrorCode? ErrorCode { get; set; }

        public PlaybackInfoResponse()
        {
            MediaSources = new List<MediaSourceInfo>();
        }
    }
}
