using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Playlists
{
    public class Playlist : Folder, IHasShares
    {
        public static string[] SupportedExtensions =
            {
                ".m3u",
                ".m3u8",
                ".pls",
                ".wpl",
                ".zpl"
            };

        public Guid OwnerUserId { get; set; }

        public Share[] Shares { get; set; }

        public Playlist()
        {
            Shares = Array.Empty<Share>();
        }

        [JsonIgnore]
        public bool IsFile => IsPlaylistFile(Path);

        public static bool IsPlaylistFile(string path)
        {
            return System.IO.Path.HasExtension(path);
        }

        [JsonIgnore]
        public override string ContainingFolderPath
        {
            get
            {
                var path = Path;

                if (IsPlaylistFile(path))
                {
                    return System.IO.Path.GetDirectoryName(path);
                }

                return path;
            }
        }

        [JsonIgnore]
        protected override bool FilterLinkedChildrenPerUser => true;

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override bool SupportsPlayedStatus => string.Equals(MediaType, "Video", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public override bool AlwaysScanInternalMetadataPath => true;

        [JsonIgnore]
        public override bool SupportsCumulativeRunTimeTicks => true;

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            return 1;
        }

        public override bool IsAuthorizedToDelete(User user, List<Folder> allCollectionFolders)
        {
            return true;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        protected override List<BaseItem> LoadChildren()
        {
            // Save a trip to the database
            return new List<BaseItem>();
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            return Task.CompletedTask;
        }

        public override List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            return GetPlayableItems(user, query);
        }

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            return new List<BaseItem>();
        }

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            return GetPlayableItems(user, query);
        }

        public IEnumerable<Tuple<LinkedChild, BaseItem>> GetManageableItems()
        {
            return GetLinkedChildrenInfos();
        }

        private List<BaseItem> GetPlayableItems(User user, InternalItemsQuery query)
        {
            if (query == null)
            {
                query = new InternalItemsQuery(user);
            }

            query.IsFolder = false;

            return base.GetChildren(user, true, query);
        }

        public static List<BaseItem> GetPlaylistItems(string playlistMediaType, IEnumerable<BaseItem> inputItems, User user, DtoOptions options)
        {
            if (user != null)
            {
                inputItems = inputItems.Where(i => i.IsVisible(user));
            }

            var list = new List<BaseItem>();

            foreach (var item in inputItems)
            {
                var playlistItems = GetPlaylistItems(item, user, playlistMediaType, options);
                list.AddRange(playlistItems);
            }

            return list;
        }

        private static IEnumerable<BaseItem> GetPlaylistItems(BaseItem item, User user, string mediaType, DtoOptions options)
        {
            if (item is MusicGenre musicGenre)
            {
                return LibraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IncludeItemTypes = new[] { typeof(Audio).Name },
                    GenreIds = new[] { musicGenre.Id },
                    OrderBy = new[] { ItemSortBy.AlbumArtist, ItemSortBy.Album, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Ascending)).ToArray(),
                    DtoOptions = options
                });
            }

            if (item is MusicArtist musicArtist)
            {
                return LibraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IncludeItemTypes = new[] { typeof(Audio).Name },
                    ArtistIds = new[] { musicArtist.Id },
                    OrderBy = new[] { ItemSortBy.AlbumArtist, ItemSortBy.Album, ItemSortBy.SortName }.Select(i => new ValueTuple<string, SortOrder>(i, SortOrder.Ascending)).ToArray(),
                    DtoOptions = options
                });
            }

            if (item is Folder folder)
            {
                var query = new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false,
                    OrderBy = new[] { (ItemSortBy.SortName, SortOrder.Ascending) },
                    MediaTypes = new[] { mediaType },
                    EnableTotalRecordCount = false,
                    DtoOptions = options
                };

                return folder.GetItemList(query);
            }

            return new[] { item };
        }

        [JsonIgnore]
        public override bool IsPreSorted => true;

        public string PlaylistMediaType { get; set; }

        [JsonIgnore]
        public override string MediaType => PlaylistMediaType;

        public void SetMediaType(string value)
        {
            PlaylistMediaType = value;
        }

        [JsonIgnore]
        private bool IsSharedItem
        {
            get
            {
                var path = Path;

                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }

                return FileSystem.ContainsSubPath(ConfigurationManager.ApplicationPaths.DataPath, path);
            }
        }

        public override bool IsVisible(User user)
        {
            if (!IsSharedItem)
            {
                return base.IsVisible(user);
            }

            if (user.Id == OwnerUserId)
            {
                return true;
            }

            var shares = Shares;
            if (shares.Length == 0)
            {
                return base.IsVisible(user);
            }

            var userId = user.Id.ToString("N", CultureInfo.InvariantCulture);
            foreach (var share in shares)
            {
                if (string.Equals(share.UserId, userId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool IsVisibleStandalone(User user)
        {
            if (!IsSharedItem)
            {
                return base.IsVisibleStandalone(user);
            }

            return IsVisible(user);
        }
    }
}
