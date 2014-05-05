using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelInfo
    {
        /// <summary>
        /// Gets the home page URL.
        /// </summary>
        /// <value>The home page URL.</value>
        public string HomePageUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance can search.
        /// </summary>
        /// <value><c>true</c> if this instance can search; otherwise, <c>false</c>.</value>
        public bool CanSearch { get; set; }

        public List<ChannelMediaType> MediaTypes { get; set; }

        public List<ChannelMediaContentType> ContentTypes { get; set; }

        public ChannelInfo()
        {
            MediaTypes = new List<ChannelMediaType>();
            ContentTypes = new List<ChannelMediaContentType>();
        }
    }

}
