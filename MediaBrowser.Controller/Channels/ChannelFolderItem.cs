using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Users;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.Channels
{
    public class ChannelFolderItem : Folder
    {
        public ChannelFolderType ChannelFolderType { get; set; }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            // Don't block. 
            return false;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.ChannelContent;
        }

        [IgnoreDataMember]
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

        protected override string CreateUserDataKey()
        {
            return ExternalId;
        }

        protected override async Task<QueryResult<BaseItem>> GetItemsInternal(InternalItemsQuery query)
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

        public override bool CanDelete()
        {
            return false;
        }

        public override bool IsVisibleStandalone(User user)
        {
            return IsVisibleStandaloneInternal(user, false) && ChannelVideoItem.IsChannelVisible(this, user);
        }
    }
}
