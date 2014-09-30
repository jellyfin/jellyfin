using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Querying;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelFolderItem : Folder, IChannelItem
    {
        public string ExternalId { get; set; }

        public string ChannelId { get; set; }
        public string DataVersion { get; set; }

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

        public override bool IsSaveLocalMetadataEnabled()
        {
            return false;
        }

        public override string GetUserDataKey()
        {
            return ExternalId;
        }

        public override async Task<QueryResult<BaseItem>> GetUserItems(UserItemsQuery query)
        {
            try
            {
                // Don't blow up here because it could cause parent screens with other content to fail
                return await ChannelManager.GetChannelItemsInternal(new ChannelItemQuery
                {
                    ChannelId = ChannelId,
                    FolderId = Id.ToString("N"),
                    Limit = query.Limit,
                    StartIndex = query.StartIndex,
                    UserId = query.User.Id.ToString("N"),
                    SortBy = query.SortBy,
                    SortOrder = query.SortOrder

                }, new Progress<double>(), CancellationToken.None);
            }
            catch
            {
                // Already logged at lower levels
                return new QueryResult<BaseItem>
                {

                };
            }
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return System.IO.Path.Combine(basePath, "channels", ChannelId, Id.ToString("N"));
        }
    }
}
