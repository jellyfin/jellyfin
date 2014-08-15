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

        protected override bool FilterLinkedChildrenPerUser
        {
            get
            {
                return true;
            }
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetPlayableItems(user);
        }

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, bool includeLinkedChildren = true)
        {
            return GetPlayableItems(user);
        }

        public IEnumerable<Tuple<LinkedChild, BaseItem>> GetManageableItems()
        {
            return GetLinkedChildrenInfos();
        }

        private IEnumerable<BaseItem> GetPlayableItems(User user)
        {
            return GetPlaylistItems(MediaType, base.GetChildren(user, true), user);
        }

        public static IEnumerable<BaseItem> GetPlaylistItems(string playlistMediaType, IEnumerable<BaseItem> inputItems, User user)
        {
            if (user != null)
            {
                inputItems = inputItems.Where(i => i.IsVisible(user));
            }

            inputItems = inputItems.SelectMany(i =>
            {
                var folder = i as Folder;

                if (folder != null)
                {
                    var items = user == null
                        ? folder.GetRecursiveChildren()
                        : folder.GetRecursiveChildren(user, true);

                    items = items
                       .Where(m => !m.IsFolder);

                    if (!folder.IsPreSorted)
                    {
                        items = LibraryManager.Sort(items, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
                    }

                    return items;
                }

                return new[] { i };

            }).Where(m =>  string.Equals(m.MediaType, playlistMediaType, StringComparison.OrdinalIgnoreCase));

            return inputItems;
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
