using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelCategoryItem : Folder, IChannelItem
    {
        public string ExternalId { get; set; }

        public ChannelItemType ChannelItemType { get; set; }

        public string OriginalImageUrl { get; set; }

        protected override bool GetBlockUnratedValue(UserConfiguration config)
        {
            // Don't block. 
            return false;
        }

        public override bool SupportsLocalMetadata
        {
            get
            {
                return false;
            }
        }
    }
}
