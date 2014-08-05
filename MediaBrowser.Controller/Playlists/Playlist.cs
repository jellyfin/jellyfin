using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Playlists
{
    public class Playlist : Folder
    {
        public string OwnerUserId { get; set; }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetPlayableItems(user);
        }

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, bool includeLinkedChildren = true)
        {
            return GetPlayableItems(user);
        }

        public IEnumerable<BaseItem> GetManageableItems()
        {
            return GetLinkedChildren();
        }

        private IEnumerable<BaseItem> GetPlayableItems(User user)
        {
            return GetPlaylistItems(MediaType, base.GetChildren(user, true), user);
        }

        public static IEnumerable<BaseItem> GetPlaylistItems(string playlistMediaType, IEnumerable<BaseItem> inputItems, User user)
        {
            return inputItems.SelectMany(i =>
            {
                var folder = i as Folder;

                if (folder != null)
                {
                    var items = folder.GetRecursiveChildren(user, true)
                        .Where(m => !m.IsFolder && string.Equals(m.MediaType, playlistMediaType, StringComparison.OrdinalIgnoreCase));

                    if (!folder.IsPreSorted)
                    {
                        items = LibraryManager.Sort(items, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
                    }

                    return items;
                }

                return new[] { i };
            });
        }

        [IgnoreDataMember]
        public override bool IsPreSorted
        {
            get
            {
                return true;
            }
        }

        public string PlaylistMediaType { get; set; }

        public override string MediaType
        {
            get
            {
                return PlaylistMediaType;
            }
        }

        public void SetMediaType(string value)
        {
            PlaylistMediaType = value;
        }

        public override bool IsVisible(User user)
        {
            return base.IsVisible(user) && string.Equals(user.Id.ToString("N"), OwnerUserId);
        }
    }
}
