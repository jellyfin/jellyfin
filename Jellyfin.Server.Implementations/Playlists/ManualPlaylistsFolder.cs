using System.Collections.Generic;
using System.Linq;
using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Playlists;
using Jellyfin.Model.Querying;
using Jellyfin.Model.Serialization;

namespace Jellyfin.Server.Implementations.Playlists
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

        [IgnoreDataMember]
        public override bool IsHidden => true;

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages => false;

        [IgnoreDataMember]
        public override string CollectionType => Jellyfin.Model.Entities.CollectionType.Playlists;

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

