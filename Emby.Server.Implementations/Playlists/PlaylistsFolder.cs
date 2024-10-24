#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Playlists
{
    [RequiresSourceSerialisation]
    public class PlaylistsFolder : BasePluginFolder
    {
        public PlaylistsFolder()
        {
            Name = "Playlists";
        }

        [JsonIgnore]
        public override bool IsHidden => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override CollectionType? CollectionType => Jellyfin.Data.Enums.CollectionType.playlists;

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return base.GetEligibleChildrenForRecursiveChildren(user).OfType<Playlist>();
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.User is null)
            {
                query.Recursive = false;
                return base.GetItemsInternal(query);
            }

            query.Recursive = true;
            query.IncludeItemTypes = new[] { BaseItemKind.Playlist };
            return QueryWithPostFiltering2(query);
        }

        public override string GetClientTypeName()
        {
            return "ManualPlaylistsFolder";
        }
    }
}
