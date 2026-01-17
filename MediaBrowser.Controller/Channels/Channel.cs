#nullable disable

#pragma warning disable CS1591

using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Querying;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.Channels
{
    public class Channel : Folder
    {
        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override SourceType SourceType => SourceType.Channel;

        public override bool IsVisible(User user, bool skipAllowedTagsCheck = false)
        {
            var blockedChannelsPreference = user.GetPreferenceValues<Guid>(PreferenceKind.BlockedChannels);
            if (blockedChannelsPreference.Length != 0)
            {
                if (blockedChannelsPreference.Contains(Id))
                {
                    return false;
                }
            }
            else
            {
                if (!user.HasPermission(PermissionKind.EnableAllChannels)
                    && !user.GetPreferenceValues<Guid>(PreferenceKind.EnabledChannels).Contains(Id))
                {
                    return false;
                }
            }

            return base.IsVisible(user, skipAllowedTagsCheck);
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            try
            {
                query.Parent = this;
                query.ChannelIds = new Guid[] { Id };

                // Don't blow up here because it could cause parent screens with other content to fail
                // Channel items are loaded asynchronously - return empty result to avoid blocking
                // Note: This is a workaround for synchronous property getters that need async data
                // TODO: Refactor to make GetItemsInternal async when callers can be updated
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ChannelManager.GetChannelItemsInternal(query, new Progress<double>(), CancellationToken.None).ConfigureAwait(false);
                    }
                    catch
                    {
                        // Silently handle errors - this is a background operation
                    }
                });
                return new QueryResult<BaseItem>();
            }
            catch
            {
                // Already logged at lower levels
                return new QueryResult<BaseItem>();
            }
        }

        /// <summary>
        /// Gets items internally asynchronously.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The query result.</returns>
        protected override async Task<QueryResult<BaseItem>> GetItemsInternalAsync(InternalItemsQuery query, CancellationToken cancellationToken)
        {
            Logger?.LogInformation("[PR16038] Channel.GetItemsInternalAsync called for {ChannelName} ({ChannelId})", Name, Id);
            try
            {
                query.Parent = this;
                query.ChannelIds = new Guid[] { Id };

                Logger?.LogInformation("[PR16038] Loading channel items asynchronously for Channel {ChannelId} (no Task.Run!)", Id);
                // Proper async implementation - no Task.Run needed!
                var result = await ChannelManager.GetChannelItemsInternal(query, new Progress<double>(), cancellationToken).ConfigureAwait(false);
                Logger?.LogInformation("[PR16038] Channel items loaded successfully: {Count} items", result.Items.Count);
                return result;
            }
            catch (Exception ex)
            {
                // Log error but don't throw - this could cause parent screens with other content to fail
                Logger.LogError(ex, "Error loading channel items for Channel {ChannelId}", Id);
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

        internal static bool IsChannelVisible(BaseItem channelItem, User user)
        {
            var channel = ChannelManager.GetChannel(channelItem.ChannelId.ToString(string.Empty));

            return channel.IsVisible(user);
        }
    }
}
