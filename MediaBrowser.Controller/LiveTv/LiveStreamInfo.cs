using MediaBrowser.Model.Entities;
using System.Collections.Generic;

namespace MediaBrowser.Controller.LiveTv
{
    public class LiveStreamInfo
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the media container.
        /// </summary>
        /// <value>The media container.</value>
        public string MediaContainer { get; set; }

        /// <summary>
        /// Gets or sets the media streams.
        /// </summary>
        /// <value>The media streams.</value>
        public List<MediaStream> MediaStreams { get; set; }

        public LiveStreamInfo()
        {
            MediaStreams = new List<MediaStream>();
        }
    }
}
