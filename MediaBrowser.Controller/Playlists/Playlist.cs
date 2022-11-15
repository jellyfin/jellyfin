#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Playlists
{
    public class Playlist : Folder, IHasShares
    {
        public static readonly IReadOnlyList<string> SupportedExtensions = new[]
        {
            ".m3u",
            ".m3u8",
            ".pls",
            ".wpl",
            ".zpl"
        };

        public Playlist()
        {
            Shares = Array.Empty<Share>();
        }

        public Guid OwnerUserId { get; set; }

        public Share[] Shares { get; set; }

        [JsonIgnore]
        public bool IsFile => IsPlaylistFile(Path);

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

        [JsonIgnore]
        public override bool IsPreSorted => true;

        public string PlaylistMediaType { get; set; }

        [JsonIgnore]
        public override string MediaType => PlaylistMediaType;

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

        public static bool IsPlaylistFile(string path)
        {
            // The path will sometimes be a directory and "Path.HasExtension" returns true if the name contains a '.' (dot).
            return System.IO.Path.HasExtension(path) && !Directory.Exists(path);
        }

        public void SetMediaType(string value)
        {
            PlaylistMediaType = value;
        }

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

        protected override Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
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
            query ??= new InternalItemsQuery(user);

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
                    IncludeItemTypes = new[] { BaseItemKind.Audio },
                    GenreIds = new[] { musicGenre.Id },
                    OrderBy = new[] { (ItemSortBy.AlbumArtist, SortOrder.Ascending), (ItemSortBy.Album, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending) },
                    DtoOptions = options
                });
            }

            if (item is MusicArtist musicArtist)
            {
                return LibraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IncludeItemTypes = new[] { BaseItemKind.Audio },
                    ArtistIds = new[] { musicArtist.Id },
                    OrderBy = new[] { (ItemSortBy.AlbumArtist, SortOrder.Ascending), (ItemSortBy.Album, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending) },
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

        public override bool IsVisible(User user)
        {
            if (!IsSharedItem)
            {
                return base.IsVisible(user);
            }

            if (user.Id.Equals(OwnerUserId))
            {
                return true;
            }

            var shares = Shares;
            if (shares.Length == 0)
            {
                return base.IsVisible(user);
            }

            var userId = user.Id;
            return shares.Any(share => Guid.TryParse(share.UserId, out var id) && id.Equals(userId));
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
