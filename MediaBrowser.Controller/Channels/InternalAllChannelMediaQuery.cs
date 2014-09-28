using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Channels
{
    public class InternalAllChannelMediaQuery
    {
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the content types.
        /// </summary>
        /// <value>The content types.</value>
        public ChannelMediaContentType[] ContentTypes { get; set; }

        /// <summary>
        /// Gets or sets the extra types.
        /// </summary>
        /// <value>The extra types.</value>
        public ExtraType[] ExtraTypes { get; set; }
        public TrailerType[] TrailerTypes { get; set; }

        public InternalAllChannelMediaQuery()
        {
            ContentTypes = new ChannelMediaContentType[] { };
            ExtraTypes = new ExtraType[] { };
            TrailerTypes = new TrailerType[] { };
        }
    }
}