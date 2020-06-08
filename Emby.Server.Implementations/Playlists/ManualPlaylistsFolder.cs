#pragma warning disable CS1591

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Playlists;
using MediaBrowser.Model.Querying;

namespace Emby.Server.Implementations.Playlists
{
    public class PlaylistsFolder : BasePluginFolder
    {
        public PlaylistsFolder()
        {
            Name = "Playlists";
        }

        public override bool IsVisible(User user)
        {
            return base.IsVisible(user) && GetChildren(user, true).Any();
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return base.GetEligibleChildrenForRecursiveChildren(user).OfType<Playlist>();
        }

        [JsonIgnore]
        public override bool IsHidden => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override string CollectionType => MediaBrowser.Model.Entities.CollectionType.Playlists;

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            if (query.User == null)
            {
                query.Recursive = false;
                return base.GetItemsInternal(query);
            }

            query.Recursive = true;
            query.IncludeItemTypes = new string[] { "Playlist" };
            query.Parent = null;
            return LibraryManager.GetItemsResult(query);
        }
    }
}
