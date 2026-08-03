#nullable disable

namespace MediaBrowser.Controller.Channels
{
    /// <summary>
    /// The request for a latest media search in a channel.
    /// </summary>
    public class ChannelLatestMediaSearch
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public string UserId { get; set; }
    }
}
