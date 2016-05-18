using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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

        [IgnoreDataMember]
        public override bool SupportsCumulativeRunTimeTicks
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
            return GetPlayableItems(user).Result;
        }

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            var items = GetPlayableItems(user).Result;

            if (query != null)
            {
                items = items.Where(i => UserViewBuilder.FilterItem(i, query));
            }

            return items;
        }

        public IEnumerable<Tuple<LinkedChild, BaseItem>> GetManageableItems()
        {
            return GetLinkedChildrenInfos();
        }

        private Task<IEnumerable<BaseItem>> GetPlayableItems(User user)
        {
            return GetPlaylistItems(MediaType, base.GetChildren(user, true), user);
        }

        public static async Task<IEnumerable<BaseItem>> GetPlaylistItems(string playlistMediaType, IEnumerable<BaseItem> inputItems, User user)
        {
            if (user != null)
            {
                inputItems = inputItems.Where(i => i.IsVisible(user));
            }

            var list = new List<BaseItem>();

            foreach (var item in inputItems)
            {
                var playlistItems = await GetPlaylistItems(item, user, playlistMediaType).ConfigureAwait(false);
                list.AddRange(playlistItems);
            }

            return list;
        }

        private static async Task<IEnumerable<BaseItem>> GetPlaylistItems(BaseItem item, User user, string mediaType)
        {
            var musicGenre = item as MusicGenre;
            if (musicGenre != null)
            {
                var items = LibraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IncludeItemTypes = new[] { typeof(Audio).Name },
                    Genres = new[] { musicGenre.Name }
                });

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
                    : user.RootFolder.GetRecursiveChildren(user, new InternalItemsQuery(user)
                    {
                        IncludeItemTypes = new[] { typeof(Audio).Name },
                        ArtistNames = new[] { musicArtist.Name }
                    });

                return LibraryManager.Sort(items, user, new[] { ItemSortBy.AlbumArtist, ItemSortBy.Album, ItemSortBy.SortName }, SortOrder.Ascending);
            }

            var folder = item as Folder;
            if (folder != null)
            {
                var query = new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false,
                    SortBy = new[] { ItemSortBy.SortName },
                    MediaTypes = new[] { mediaType },
                    EnableTotalRecordCount = false
                };

                var itemsResult = await folder.GetItems(query).ConfigureAwait(false);
                var items = itemsResult.Items;

                return items;
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
