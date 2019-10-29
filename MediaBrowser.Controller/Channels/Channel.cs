using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Channels
{
    public class Channel : Folder
    {
        public override bool IsVisible(User user)
        {
            if (user.Policy.BlockedChannels != null)
            {
                if (user.Policy.BlockedChannels.Contains(Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            else
            {
                if (!user.Policy.EnableAllChannels && !user.Policy.EnabledChannels.Contains(Id.ToString("N", CultureInfo.InvariantCulture), StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return base.IsVisible(user);
        }

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override SourceType SourceType => SourceType.Channel;

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            try
            {
                query.Parent = this;
                query.ChannelIds = new Guid[] { Id };

                // Don't blow up here because it could cause parent screens with other content to fail
                return ChannelManager.GetChannelItemsInternal(query, new SimpleProgress<double>(), CancellationToken.None).Result;
            }
            catch
            {
                // Already logged at lower levels
                return new QueryResult<BaseItem>();
            }
        }

        protected override string GetInternalMetadataPath(string basePath)
        {
            return GetInternalMetadataPath(basePath, Id);
        }

        public static string GetInternalMetadataPath(string basePath, Guid id)
        {
            return System.IO.Path.Combine(basePath, "channels", id.ToString("N", CultureInfo.InvariantCulture), "metadata");
        }

        public override bool CanDelete()
        {
            return false;
        }

        protected override bool IsAllowTagFilterEnforced()
        {
            return false;
        }

        internal static bool IsChannelVisible(BaseItem channelItem, User user)
        {
            var channel = ChannelManager.GetChannel(channelItem.ChannelId.ToString(""));

            return channel.IsVisible(user);
        }
    }
}
