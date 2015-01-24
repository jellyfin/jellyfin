using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Querying;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Channels
{
    public class Channel : Folder
    {
        public string OriginalChannelName { get; set; }

        public override bool IsVisible(User user)
        {
            if (user.Policy.BlockedChannels != null)
            {
                if (user.Policy.BlockedChannels.Contains(Id.ToString("N"), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            else
            {
                if (!user.Policy.EnableAllChannels && !user.Policy.EnabledChannels.Contains(Id.ToString("N"), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            
            return base.IsVisible(user);
        }

        public override async Task<QueryResult<BaseItem>> GetItems(InternalItemsQuery query)
        {
            try
            {
                // Don't blow up here because it could cause parent screens with other content to fail
                return await ChannelManager.GetChannelItemsInternal(new ChannelItemQuery
                {
                    ChannelId = Id.ToString("N"),
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
            return GetInternalMetadataPath(basePath, Id);
        }

        public static string GetInternalMetadataPath(string basePath, Guid id)
        {
            return System.IO.Path.Combine(basePath, "channels", id.ToString("N"), "metadata");
        }
    }
}
