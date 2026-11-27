#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Api.Extensions;
using Jellyfin.Data;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    public class CollectionFolderImageProvider : BaseDynamicImageProvider<CollectionFolder>
    {
        internal const int CollageItemLimit = 8;

        // Lists the items a collage pictures, so it can be rebuilt as soon as one of them is blocked.
        internal const string CollageStateFileName = "collage-state.json";

        private readonly ILibraryManager _libraryManager;
        private readonly IUserManager _userManager;

        public CollectionFolderImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, ILibraryManager libraryManager, IUserManager userManager) : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _libraryManager = libraryManager;
            _userManager = userManager;
        }

        // A collage that would only picture blocked items must go, rather than keep showing them.
        protected override bool RemoveImageWhenEmpty => true;

        /// <summary>
        /// Gets the tags blocked by any of the given users, normalized the same way the library normalizes tags.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <returns>The blocked tags.</returns>
        internal static string[] GetBlockedTags(IEnumerable<User> users)
        {
            return users
                .SelectMany(user => user.GetPreference(PreferenceKind.BlockedTags))
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.GetCleanValue())
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        /// <summary>
        /// Gets whether an item carries, directly or through a parent or its library, one of the given tags.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <param name="blockedTags">The blocked tags, as returned by <see cref="GetBlockedTags(IEnumerable{User})"/>.</param>
        /// <returns><c>true</c> if the item is blocked.</returns>
        internal static bool IsBlocked(BaseItem item, string[] blockedTags)
        {
            return blockedTags.Length > 0
                && item.GetInheritedTags().Any(tag => blockedTags.Contains(tag.GetCleanValue(), StringComparer.Ordinal));
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var view = (CollectionFolder)item;
            var viewType = view.CollectionType;
            var blockedTags = GetBlockedTags(view);

            if (viewType == CollectionType.music)
            {
                // Music albums usually don't have dedicated backdrops, so use artist instead.
                // Artists carry no library of their own, so an item query for them is not
                // restricted to this library and would collage the artists of every music
                // library. Resolve them through the tracks that credit them instead.
                return GetArtistsWithImages(view, blockedTags);
            }

            // A box set is only excluded by the query when it is tagged itself, so the ones made up
            // entirely of blocked items are filtered out afterwards, and all of them are fetched for that.
            var filterBoxSets = viewType == CollectionType.boxsets && blockedTags.Length > 0;

            var items = view.GetItemList(new InternalItemsQuery
            {
                CollapseBoxSetItems = false,
                Recursive = viewType != CollectionType.playlists,
                DtoOptions = new DtoOptions(false),
                ImageTypes = [ImageType.Primary],
                Limit = filterBoxSets ? null : CollageItemLimit,
                OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)],
                IncludeItemTypes = DtoExtensions.GetBaseItemKindsForCollectionType(viewType),
                ExcludeInheritedTags = blockedTags
            });

            if (!filterBoxSets)
            {
                return items;
            }

            return items
                .Where(i => !IsBoxSetOfBlockedItems(i, blockedTags))
                .Take(CollageItemLimit)
                .ToList();
        }

        private IReadOnlyList<BaseItem> GetArtistsWithImages(CollectionFolder view, string[] blockedTags)
        {
            // The blocked tags leave out both an artist tagged itself and one credited only by blocked tracks.
            return _libraryManager.GetAllArtists(new InternalItemsQuery
            {
                AncestorIds = [view.Id],
                DtoOptions = new DtoOptions(false),
                EnableTotalRecordCount = false,
                ImageTypes = [ImageType.Primary],
                Limit = CollageItemLimit,
                OrderBy = [(ItemSortBy.Random, SortOrder.Ascending)],
                ExcludeInheritedTags = blockedTags
            }).Items.Select(i => i.Item).ToArray();
        }

        protected override bool Supports(BaseItem item)
        {
            return item is CollectionFolder;
        }

        protected override string CreateImage(BaseItem item, IReadOnlyCollection<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            if (imageType == ImageType.Primary)
            {
                if (itemsWithImages.Count == 0)
                {
                    return null;
                }

                return CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540);
            }

            return base.CreateImage(item, itemsWithImages, outputPath, imageType, imageIndex);
        }

        protected override void OnImageSaved(BaseItem item, IReadOnlyList<BaseItem> itemsWithImages, ImageType imageType)
        {
            var path = GetCollageStatePath(item);
            var state = new CollageState { ItemIds = itemsWithImages.Select(i => i.Id).ToArray() };

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonSerializer.Serialize(state, JsonDefaults.Options));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // The collage is saved by now, so failing here would only throw away that the refresh updated
                // it. Without a readable record the collage is rebuilt at the next check, which is the safe side.
            }
        }

        // Besides its age, a collage is also outdated once it pictures something that is now blocked.
        protected override bool HasChangedByDate(BaseItem item, ItemImageInfo image)
        {
            var age = DateTime.UtcNow - image.DateModified;
            return age.TotalDays > 7 || PicturesBlockedItem((CollectionFolder)item);
        }

        // The collage is a single image shared by everyone who can see the library, so it leaves out whatever any
        // of them has blocked. Nobody else ever sees it, and that includes everyone who blocks a tag the library
        // itself carries, whose tags would otherwise leave the collage with nothing at all.
        private string[] GetBlockedTags(CollectionFolder view)
            => GetBlockedTags(_userManager.GetUsers().Where(user => view.IsVisible(user)));

        private bool PicturesBlockedItem(CollectionFolder view)
        {
            var blockedTags = GetBlockedTags(view);
            if (blockedTags.Length == 0)
            {
                return false;
            }

            var itemIds = ReadCollageState(view)?.ItemIds;
            if (itemIds is null)
            {
                // Nothing records what this collage pictures, so it may picture anything.
                return true;
            }

            // Mirrors what GetItemsWithImages leaves out. An item that is gone can no longer be checked, so it counts too.
            return itemIds.Any(id => _libraryManager.GetItemById(id) is not { } pictured
                || IsBlocked(pictured, blockedTags)
                || IsBoxSetOfBlockedItems(pictured, blockedTags)
                || (pictured is MusicArtist artist && !IsCreditedByUnblockedItem(view, artist, blockedTags)));
        }

        // An artist belongs to no library and is collaged through the items crediting it, so it stays only as
        // long as one of them in this library is not blocked.
        private bool IsCreditedByUnblockedItem(CollectionFolder view, MusicArtist artist, string[] blockedTags)
        {
            return _libraryManager.GetCount(new InternalItemsQuery
            {
                ParentId = view.Id,
                Recursive = true,
                ArtistIds = [artist.Id],
                ExcludeInheritedTags = blockedTags
            }) > 0;
        }

        private static bool IsBoxSetOfBlockedItems(BaseItem item, string[] blockedTags)
        {
            if (item is not BoxSet boxSet)
            {
                return false;
            }

            var children = boxSet.GetLinkedChildren();
            return children.Count > 0 && children.All(child => IsBlocked(child, blockedTags));
        }

        private static string GetCollageStatePath(BaseItem item)
            => Path.Combine(item.GetInternalMetadataPath(), CollageStateFileName);

        private static CollageState ReadCollageState(BaseItem item)
        {
            try
            {
                return JsonSerializer.Deserialize<CollageState>(File.ReadAllText(GetCollageStatePath(item)), JsonDefaults.Options);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                return null;
            }
        }

        private sealed class CollageState
        {
            public Guid[] ItemIds { get; set; }
        }
    }
}
