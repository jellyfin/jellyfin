#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Querying;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder, IHasCollectionType
    {
        public static ITVSeriesManager TVSeriesManager;

        private static readonly string[] UserSpecificViewTypes = {Model.Entities.CollectionType.Playlists};

        private static readonly string[] ViewTypesEligibleForGrouping = {Model.Entities.CollectionType.Movies, Model.Entities.CollectionType.TvShows, string.Empty};

        private static readonly string[] OriginalFolderViewTypes = {Model.Entities.CollectionType.Books, Model.Entities.CollectionType.MusicVideos, Model.Entities.CollectionType.HomeVideos, Model.Entities.CollectionType.Photos, Model.Entities.CollectionType.Music, Model.Entities.CollectionType.BoxSets};

        public string ViewType { get; set; }

        public new Guid DisplayParentId { get; set; }

        public Guid? UserId { get; set; }

        [JsonIgnore] public override bool SupportsInheritedParentImages => false;

        [JsonIgnore] public override bool SupportsPlayedStatus => false;

        [JsonIgnore] public override bool SupportsPeople => false;

        /// <inheritdoc />
        [JsonIgnore]
        public string CollectionType => ViewType;

        /// <inheritdoc />
        public override IEnumerable<Guid> GetIdsForAncestorQuery()
        {
            if (!DisplayParentId.Equals(Guid.Empty))
            {
                yield return DisplayParentId;
            }
            else if (!ParentId.Equals(Guid.Empty))
            {
                yield return ParentId;
            }
            else
            {
                yield return Id;
            }
        }

        public override int GetChildCount(User user)
        {
            return GetChildren(user, true).Count;
        }

        public override List<BaseItem> GetChildren(User user, bool includeLinkedChildren, InternalItemsQuery query)
        {
            if (query == null)
            {
                query = new InternalItemsQuery(user);
            }

            query.EnableTotalRecordCount = false;
            var result = GetItemList(query);

            return result.ToList();
        }

        public override bool CanDelete()
        {
            return false;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, InternalItemsQuery query)
        {
            query.SetUser(user);
            query.Recursive = true;
            query.EnableTotalRecordCount = false;
            query.ForceDirect = true;

            return GetItemList(query);
        }

        public static bool IsUserSpecific(Folder folder)
        {
            var collectionFolder = folder as ICollectionFolder;

            if (collectionFolder == null)
            {
                return false;
            }

            var supportsUserSpecific = folder as ISupportsUserSpecificView;
            if (supportsUserSpecific != null && supportsUserSpecific.EnableUserSpecificView)
            {
                return true;
            }

            return UserSpecificViewTypes.Contains(collectionFolder.CollectionType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsEligibleForGrouping(Folder folder)
        {
            return folder is ICollectionFolder collectionFolder
                   && IsEligibleForGrouping(collectionFolder.CollectionType);
        }

        public static bool IsEligibleForGrouping(string viewType)
        {
            return ViewTypesEligibleForGrouping.Contains(viewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        public static bool EnableOriginalFolder(string viewType)
        {
            return OriginalFolderViewTypes.Contains(viewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        protected override QueryResult<BaseItem> GetItemsInternal(InternalItemsQuery query)
        {
            var parent = this as Folder;

            if (!DisplayParentId.Equals(Guid.Empty))
            {
                parent = LibraryManager.GetItemById(DisplayParentId) as Folder ?? parent;
            }
            else if (!ParentId.Equals(Guid.Empty))
            {
                parent = LibraryManager.GetItemById(ParentId) as Folder ?? parent;
            }

            return new UserViewBuilder(UserViewManager, LibraryManager, Logger, UserDataManager, TVSeriesManager, ConfigurationManager)
                .GetUserItems(parent, this, CollectionType, query);
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return GetChildren(user, false);
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, MetadataRefreshOptions refreshOptions, IDirectoryService directoryService)
        {
            return Task.CompletedTask;
        }
    }
}
