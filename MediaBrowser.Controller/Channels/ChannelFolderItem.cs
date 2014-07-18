using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelFolderItem : Folder, IChannelItem
    {
        public string ExternalId { get; set; }

        public string ChannelId { get; set; }

        public ChannelItemType ChannelItemType { get; set; }
        public ChannelFolderType ChannelFolderType { get; set; }

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

        public override string GetUserDataKey()
        {
            return ExternalId;
        }
    }
}
