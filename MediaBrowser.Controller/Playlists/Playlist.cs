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

            return inputItems.SelectMany(i => GetPlaylistItems(i, user))
                .Where(m =>  string.Equals(m.MediaType, playlistMediaType, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<BaseItem> GetPlaylistItems(BaseItem i, User user)
        {
            var musicGenre = i as MusicGenre;
            if (musicGenre != null)
            {
                var songs = user.RootFolder
                    .GetRecursiveChildren(user)
                    .OfType<Audio>()
                    .Where(a => a.Genres.Contains(musicGenre.Name, StringComparer.OrdinalIgnoreCase));

                return LibraryManager.Sort(songs, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
            }

            var musicArtist = i as MusicArtist;
            if (musicArtist != null)
            {
                var songs = user.RootFolder
                    .GetRecursiveChildren(user)
                    .OfType<Audio>()
                    .Where(a => a.HasArtist(musicArtist.Name));

                return LibraryManager.Sort(songs, user, new[] { ItemSortBy.SortName }, SortOrder.Ascending);
            }
            
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
