using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Playlists
{
    public class Playlist : Folder, IHasShares
    {
        public string OwnerUserId { get; set; }

        public List<Share> Shares { get; set; }

        public Playlist()
        {
            Shares = new List<Share>();
        }

        [IgnoreDataMember]
        protected override bool FilterLinkedChildrenPerUser
        {
            get
            {
                return true;
            }
        }

        [IgnoreDataMember]
        public override bool AlwaysScanInternalMetadataPath
        {
            get
            {
                return true;
            }
        }

        public override bool IsAuthorizedToDelete(User user)
        {
            return true;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            return GetPlayableItems(user);
        }

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, Func<BaseItem, bool> filter)
        {
            var items = GetPlayableItems(user);

            if (filter != null)
            {
                items = items.Where(filter);
            }

            return items;
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

            return inputItems.SelectMany(i => GetPlaylistItems(i, user))
                .Where(m => string.Equals(m.MediaType, playlistMediaType, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<BaseItem> GetPlaylistItems(BaseItem item, User user)
        {
            var musicGenre = item as MusicGenre;
            if (musicGenre != null)
            {
                Func<BaseItem, bool> filter = i => i is Audio && i.Genres.Contains(musicGenre.Name, StringComparer.OrdinalIgnoreCase);

                var items = user == null
                    ? LibraryManager.RootFolder.GetRecursiveChildren(filter)
                    : user.RootFolder.GetRecursiveChildren(user, filter);

                return LibraryManager.Sort(items, user, new[] { ItemSortBy.AlbumArtist, ItemSortBy.Album, ItemSortBy.SortName }, SortOrder.Ascending);
            }

            var musicArtist = item as MusicArtist;
            if (musicArtist != null)
            {
                Func<BaseItem, bool> filter = i =>
                {
                    var audio = i as Audio;
                    return audio != null && audio.HasAnyArtist(musicArtist.Name);
                };

                var items = user == null
                    ? LibraryManager.RootFolder.GetRecursiveChildren(filter)
                    : user.RootFolder.GetRecursiveChildren(user, filter);

                return LibraryManager.Sort(items, user, new[] { ItemSortBy.AlbumArtist, ItemSortBy.Album, ItemSortBy.SortName }, SortOrder.Ascending);
            }

            var folder = item as Folder;
            if (folder != null)
            {
                var items = user == null
                    ? folder.GetRecursiveChildren(m => !m.IsFolder)
                    : folder.GetRecursiveChildren(user, m => !m.IsFolder);

                if (folder.IsPreSorted)
                {
                    return items;
                }
                return LibraryManager.Sort(items, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
            }

            return new[] { item };
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

        [IgnoreDataMember]
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
            if (base.IsVisible(user))
            {
                var userId = user.Id.ToString("N");

                return Shares.Any(i => string.Equals(userId, i.UserId, StringComparison.OrdinalIgnoreCase)) ||
                    string.Equals(OwnerUserId, userId, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
