using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder
    {
        public string ViewType { get; set; }
        public Guid DisplayParentId { get; set; }

        public Guid? UserId { get; set; }

        public static ITVSeriesManager TVSeriesManager;
        public static IPlaylistManager PlaylistManager;

        public bool ContainsDynamicCategories(User user)
        {
            return true;
        }

        public override IEnumerable<Guid> GetIdsForAncestorQuery()
        {
            var list = new List<Guid>();

            if (DisplayParentId != Guid.Empty)
            {
                list.Add(DisplayParentId);
            }
            else if (ParentId != Guid.Empty)
            {
                list.Add(ParentId);
            }
            else
            {
                list.Add(Id);
            }
            return list;
        }

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus
        {
            get
            {
                return false;
            }
        }

        public override int GetChildCount(User user)
        {
            return GetChildren(user, true).Count();
        }

        protected override Task<QueryResult<BaseItem>> GetItemsInternal(InternalItemsQuery query)
        {
            var parent = this as Folder;

            if (DisplayParentId != Guid.Empty)
            {
                parent = LibraryManager.GetItemById(DisplayParentId) as Folder ?? parent;
            }
            else if (ParentId != Guid.Empty)
            {
                parent = LibraryManager.GetItemById(ParentId) as Folder ?? parent;
            }

            return new UserViewBuilder(UserViewManager, LiveTvManager, ChannelManager, LibraryManager, Logger, UserDataManager, TVSeriesManager, ConfigurationManager, PlaylistManager)
                .GetUserItems(parent, this, ViewType, query);
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var result = GetItems(new InternalItemsQuery
            {
                User = user,
                EnableTotalRecordCount = false

            }).Result;

            return result.Items;
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
            var result = GetItems(new InternalItemsQuery
            {
                User = user,
                Recursive = true,
                EnableTotalRecordCount = false,

                ForceDirect = true

            }).Result;

            return result.Items.Where(i => UserViewBuilder.FilterItem(i, query));
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return GetChildren(user, false);
        }

        public static bool IsUserSpecific(Folder folder)
        {
            var standaloneTypes = new List<string>
            {
                CollectionType.Playlists
            };

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

            return standaloneTypes.Contains(collectionFolder.CollectionType ?? string.Empty);
        }

        public static bool IsEligibleForGrouping(Folder folder)
        {
            var collectionFolder = folder as ICollectionFolder;
            return collectionFolder != null && IsEligibleForGrouping(collectionFolder.CollectionType);
        }

        public static bool IsEligibleForGrouping(string viewType)
        {
            var types = new[] 
            { 
                CollectionType.Movies, 
                CollectionType.TvShows,
                string.Empty
            };

            return types.Contains(viewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        public static bool IsEligibleForEnhancedView(string viewType)
        {
            var types = new[] 
            { 
                CollectionType.Movies, 
                CollectionType.TvShows 
            };

            return types.Contains(viewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        public static bool EnableOriginalFolder(string viewType)
        {
            var types = new[] 
            { 
                CollectionType.Games, 
                CollectionType.Books, 
                CollectionType.MusicVideos, 
                CollectionType.HomeVideos, 
                CollectionType.Photos, 
                CollectionType.Music, 
                CollectionType.BoxSets
            };

            return types.Contains(viewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        protected override Task ValidateChildrenInternal(IProgress<double> progress, System.Threading.CancellationToken cancellationToken, bool recursive, bool refreshChildMetadata, Providers.MetadataRefreshOptions refreshOptions, Providers.IDirectoryService directoryService)
        {
            return Task.FromResult(true);
        }

        [IgnoreDataMember]
        public override bool SupportsPeople
        {
            get
            {
                return false;
            }
        }
    }
}
