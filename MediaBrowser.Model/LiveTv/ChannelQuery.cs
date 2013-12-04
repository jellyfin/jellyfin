
namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ChannelQuery.
    /// </summary>
    public class ChannelQuery
    {
        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType? ChannelType { get; set; }

        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }
    }
}
