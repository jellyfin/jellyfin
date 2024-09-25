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
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Playlists
{
    public class Playlist : Folder, IHasShares
    {
        public static readonly IReadOnlyList<string> SupportedExtensions =
        [
            ".m3u",
            ".m3u8",
            ".pls",
            ".wpl",
            ".zpl"
        ];

        public Playlist()
        {
            Shares = [];
            OpenAccess = false;
        }

        public Guid OwnerUserId { get; set; }

        public bool OpenAccess { get; set; }

        public IReadOnlyList<PlaylistUserPermissions> Shares { get; set; }

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
        public override bool SupportsPlayedStatus => MediaType == Jellyfin.Data.Enums.MediaType.Video;

        [JsonIgnore]
        public override bool AlwaysScanInternalMetadataPath => true;

        [JsonIgnore]
        public override bool SupportsCumulativeRunTimeTicks => true;

        [JsonIgnore]
        public override bool IsPreSorted => true;

        public MediaType PlaylistMediaType { get; set; }

        [JsonIgnore]
        public override MediaType MediaType => PlaylistMediaType;

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

        public void SetMediaType(MediaType? value)
        {
            PlaylistMediaType = value ?? MediaType.Unknown;
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
            return [];
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, bool allowRemoveRoot, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public override List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            return GetPlayableItems(user, query);
        }

        protected override IEnumerable<BaseItem> GetNonCachedChildren(IDirectoryService directoryService)
        {
            return [];
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

        public static IReadOnlyList<BaseItem> GetPlaylistItems(IEnumerable<BaseItem> inputItems, User user, DtoOptions options)
        {
            if (user is not null)
            {
                inputItems = inputItems.Where(i => i.IsVisible(user));
            }

            var list = new List<BaseItem>();

            foreach (var item in inputItems)
            {
                var playlistItems = GetPlaylistItems(item, user, options);
                list.AddRange(playlistItems);
            }

            return list;
        }

        private static IEnumerable<BaseItem> GetPlaylistItems(BaseItem item, User user, DtoOptions options)
        {
            if (item is MusicGenre musicGenre)
            {
                return LibraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IncludeItemTypes = [BaseItemKind.Audio],
                    GenreIds = [musicGenre.Id],
                    OrderBy = [(ItemSortBy.AlbumArtist, SortOrder.Ascending), (ItemSortBy.Album, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending)],
                    DtoOptions = options
                });
            }

            if (item is MusicArtist musicArtist)
            {
                return LibraryManager.GetItemList(new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IncludeItemTypes = [BaseItemKind.Audio],
                    ArtistIds = [musicArtist.Id],
                    OrderBy = [(ItemSortBy.AlbumArtist, SortOrder.Ascending), (ItemSortBy.Album, SortOrder.Ascending), (ItemSortBy.SortName, SortOrder.Ascending)],
                    DtoOptions = options
                });
            }

            if (item is Folder folder)
            {
                var query = new InternalItemsQuery(user)
                {
                    Recursive = true,
                    IsFolder = false,
                    MediaTypes = [MediaType.Audio, MediaType.Video],
                    EnableTotalRecordCount = false,
                    DtoOptions = options
                };

                return folder.GetItemList(query);
            }

            return [item];
        }

        public override bool IsVisible(User user, bool skipAllowedTagsCheck = false)
        {
            if (!IsSharedItem)
            {
                return base.IsVisible(user, skipAllowedTagsCheck);
            }

            if (OpenAccess)
            {
                return true;
            }

            var userId = user.Id;
            if (userId.Equals(OwnerUserId))
            {
                return true;
            }

            var shares = Shares;
            if (shares.Count == 0)
            {
                return false;
            }

            return shares.Any(s => s.UserId.Equals(userId));
        }

        public override bool CanDelete(User user)
        {
            return user.HasPermission(PermissionKind.IsAdministrator) || user.Id.Equals(OwnerUserId);
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
