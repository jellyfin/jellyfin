using System.Collections.Generic;

namespace MediaBrowser.Model.Channels
{
    public class ChannelFeatures
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance can search.
        /// </summary>
        /// <value><c>true</c> if this instance can search; otherwise, <c>false</c>.</value>
        public bool CanSearch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can index all media.
        /// </summary>
        /// <value><c>true</c> if this instance can index all media; otherwise, <c>false</c>.</value>
        public bool CanGetAllMedia { get; set; }
        
        /// <summary>
        /// Gets or sets the media types.
        /// </summary>
        /// <value>The media types.</value>
        public List<ChannelMediaType> MediaTypes { get; set; }

        /// <summary>
        /// Gets or sets the content types.
        /// </summary>
        /// <value>The content types.</value>
        public List<ChannelMediaContentType> ContentTypes { get; set; }

        /// <summary>
        /// Represents the maximum number of records the channel allows retrieving at a time
        /// </summary>
        public int? MaxPageSize { get; set; }

        public ChannelFeatures()
        {
            MediaTypes = new List<ChannelMediaType>();
            ContentTypes = new List<ChannelMediaContentType>();
        }
    }

}
