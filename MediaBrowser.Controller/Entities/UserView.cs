#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder, IHasCollectionType
    {
        private static readonly CollectionType?[] _viewTypesEligibleForGrouping =
        {
            Jellyfin.Data.Enums.CollectionType.movies,
            Jellyfin.Data.Enums.CollectionType.tvshows,
            null
        };

        private static readonly CollectionType?[] _originalFolderViewTypes =
        {
            Jellyfin.Data.Enums.CollectionType.books,
            Jellyfin.Data.Enums.CollectionType.musicvideos,
            Jellyfin.Data.Enums.CollectionType.homevideos,
            Jellyfin.Data.Enums.CollectionType.photos,
            Jellyfin.Data.Enums.CollectionType.music,
            Jellyfin.Data.Enums.CollectionType.boxsets
        };

        public static ITVSeriesManager TVSeriesManager { get; set; }

        /// <summary>
        /// Gets or sets the view type.
        /// </summary>
        public CollectionType? ViewType { get; set; }

        /// <summary>
        /// Gets or sets the display parent id.
        /// </summary>
        public new Guid DisplayParentId { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        public Guid? UserId { get; set; }

        /// <inheritdoc />
        [JsonIgnore]
        public CollectionType? CollectionType => ViewType;

        /// <inheritdoc />
        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        /// <inheritdoc />
        [JsonIgnore]
        public override bool SupportsPlayedStatus => false;

        /// <inheritdoc />
        [JsonIgnore]
        public override bool SupportsPeople => false;

        /// <inheritdoc />
        public override IEnumerable<Guid> GetIdsForAncestorQuery()
        {
            if (!DisplayParentId.IsEmpty())
            {
                yield return DisplayParentId;
            }
            else if (!ParentId.IsEmpty())
            {
                yield return ParentId;
            }
            else
            {
                yield return Id;
            }
        }

        /// <inheritdoc />
        public override int GetChildCount(User user)
        {
            return GetChildren(user, true).Count;
        }

        /// <inheritdoc />
        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            var parent = this as Folder;

            if (!DisplayParentId.IsEmpty())
            {
                parent = LibraryManager.GetItemById(DisplayParentId) as Folder ?? parent;
            }
            else if (!ParentId.IsEmpty())
            {
                parent = LibraryManager.GetItemById(ParentId) as Folder ?? parent;
            }

            return new UserViewBuilder(UserViewManager, LibraryManager, Logger, UserDataManager, TVSeriesManager)
                .GetUserItems(parent, this, CollectionType, query);
        }

        /// <inheritdoc />
        public override List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            query ??= new InternalItemsQuery(user);

            query.EnableTotalRecordCount = false;
            var result = GetItemList(query);

            return result.ToList();
        }

        /// <inheritdoc />
        public override bool CanDelete()
        {
            return false;
        }

        /// <inheritdoc />
        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        /// <inheritdoc />
        public override IReadOnlyList<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            query.SetUser(user);
            query.Recursive = true;
            query.EnableTotalRecordCount = false;
            query.ForceDirect = true;

            return GetItemList(query);
        }

        /// <inheritdoc />
        protected override IReadOnlyList<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return GetChildren(user, false);
        }

        public static bool IsUserSpecific(Folder folder)
        {
            if (folder is not ICollectionFolder collectionFolder)
            {
                return false;
            }

            if (folder is ISupportsUserSpecificView supportsUserSpecific
                && supportsUserSpecific.EnableUserSpecificView)
            {
                return true;
            }

            return collectionFolder.CollectionType == Jellyfin.Data.Enums.CollectionType.playlists;
        }

        public static bool IsEligibleForGrouping(Folder folder)
        {
            return folder is ICollectionFolder collectionFolder
                    && IsEligibleForGrouping(collectionFolder.CollectionType);
        }

        public static bool IsEligibleForGrouping(CollectionType? viewType)
        {
            return _viewTypesEligibleForGrouping.Contains(viewType);
        }

        public static bool EnableOriginalFolder(CollectionType? viewType)
        {
            return _originalFolderViewTypes.Contains(viewType);
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, bool allowRemoveRoot, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
