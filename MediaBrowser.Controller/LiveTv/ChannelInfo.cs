using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    /// <summary>
    /// Class ChannelInfo
    /// </summary>
    public class ChannelInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the number.
        /// </summary>
        /// <value>The number.</value>
        public string Number { get; set; }

        /// <summary>
        /// Get or sets the Id.
        /// </summary>
        /// <value>The id of the channel.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType ChannelType { get; set; }

        /// <summary>
        /// Set this value to true or false if it is known via channel info whether there is an image or not.
        /// Leave it null if the only way to determine is by requesting the image and handling the failure.
        /// </summary>
        public bool? HasImage { get; set; }
    }
}
