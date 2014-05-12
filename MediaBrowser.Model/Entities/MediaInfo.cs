using System.Collections.Generic;

namespace MediaBrowser.Model.Entities
{
    public class MediaInfo
    {
        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        public List<MediaStream> MediaStreams { get; set; }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        public string Format { get; set; }

        public int? TotalBitrate { get; set; }

        public MediaInfo()
        {
            MediaStreams = new List<MediaStream>();
        }
    }
}