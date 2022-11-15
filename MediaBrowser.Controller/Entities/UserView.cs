#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using Jellyfin.Extensions;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder, IHasCollectionType
    {
        private static readonly string[] _viewTypesEligibleForGrouping = new string[]
        {
            Model.Entities.CollectionType.Movies,
            Model.Entities.CollectionType.TvShows,
            string.Empty
        };

        private static readonly string[] _originalFolderViewTypes = new string[]
        {
            Model.Entities.CollectionType.Books,
            Model.Entities.CollectionType.MusicVideos,
            Model.Entities.CollectionType.HomeVideos,
            Model.Entities.CollectionType.Photos,
            Model.Entities.CollectionType.Music,
            Model.Entities.CollectionType.BoxSets
        };

        public static ITVSeriesManager TVSeriesManager { get; set; }

        /// <summary>
        /// Gets or sets the view type.
        /// </summary>
        public string ViewType { get; set; }

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
        public string CollectionType => ViewType;

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
            if (!DisplayParentId.Equals(default))
            {
                yield return DisplayParentId;
            }
            else if (!ParentId.Equals(default))
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

            if (!DisplayParentId.Equals(default))
            {
                parent = LibraryManager.GetItemById(DisplayParentId) as Folder ?? parent;
            }
            else if (!ParentId.Equals(default))
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
        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            query.SetUser(user);
            query.Recursive = true;
            query.EnableTotalRecordCount = false;
            query.ForceDirect = true;

            return GetItemList(query);
        }

        /// <inheritdoc />
        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
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

            return string.Equals(Model.Entities.CollectionType.Playlists, collectionFolder.CollectionType, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsEligibleForGrouping(Folder folder)
        {
            return folder is ICollectionFolder collectionFolder
                    && IsEligibleForGrouping(collectionFolder.CollectionType);
        }

        public static bool IsEligibleForGrouping(string viewType)
        {
            return _viewTypesEligibleForGrouping.Contains(viewType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        public static bool EnableOriginalFolder(string viewType)
        {
            return _originalFolderViewTypes.Contains(viewType ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, bool recursive, bool refreshChildMetadata, Providers.MetadataRefreshOptions refreshOptions, Providers.IDirectoryService directoryService, System.Threading.CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
