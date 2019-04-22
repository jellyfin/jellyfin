using Jellyfin.Model.Dlna;
using Jellyfin.Model.Dto;

namespace Jellyfin.Model.MediaInfo
{
    public class PlaybackInfoResponse
    {
        /// <summary>
        /// Gets or sets the media sources.
        /// </summary>
        /// <value>The media sources.</value>
        public MediaSourceInfo[] MediaSources { get; set; }

        /// <summary>
        /// Gets or sets the play session identifier.
        /// </summary>
        /// <value>The play session identifier.</value>
        public string PlaySessionId { get; set; }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        /// <value>The error code.</value>
        public PlaybackErrorCode? ErrorCode { get; set; }

        public PlaybackInfoResponse()
        {
            MediaSources = new MediaSourceInfo[] { };
        }
    }
}
