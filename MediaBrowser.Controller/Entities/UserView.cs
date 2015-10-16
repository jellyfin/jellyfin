using MediaBrowser.Controller.Playlists;
using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder
    {
        public string ViewType { get; set; }
        public Guid ParentId { get; set; }
        public Guid DisplayParentId { get; set; }

        public Guid? UserId { get; set; }
        
        public static ITVSeriesManager TVSeriesManager;
        public static IPlaylistManager PlaylistManager;

        public bool ContainsDynamicCategories(User user)
        {
            return true;
        }
        
        public override Task<QueryResult<BaseItem>> GetItems(InternalItemsQuery query)
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

            return new UserViewBuilder(UserViewManager, LiveTvManager, ChannelManager, LibraryManager, Logger, UserDataManager, TVSeriesManager, CollectionManager, PlaylistManager)
                .GetUserItems(parent, this, ViewType, query);
        }

        public override IEnumerable<BaseItem> GetChildren(User user, bool includeLinkedChildren)
        {
            var result = GetItems(new InternalItemsQuery
            {
                User = user

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

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, Func<BaseItem, bool> filter)
        {
            var result = GetItems(new InternalItemsQuery
            {
                User = user,
                Recursive = true,
                Filter = filter

            }).Result;

            return result.Items;
        }

        protected override IEnumerable<BaseItem> GetEligibleChildrenForRecursiveChildren(User user)
        {
            return GetChildren(user, false);
        }

        public static bool IsExcludedFromGrouping(Folder folder)
        {
            var standaloneTypes = new List<string>
            {
                CollectionType.Books,
                CollectionType.HomeVideos,
                CollectionType.Photos,
                CollectionType.Playlists,
                CollectionType.BoxSets,
                CollectionType.MusicVideos
            };

            var collectionFolder = folder as ICollectionFolder;

            if (collectionFolder == null)
            {
                return false;
            }

            return standaloneTypes.Contains(collectionFolder.CollectionType ?? string.Empty);
        }

        public static bool IsUserSpecific(Folder folder)
        {
            var standaloneTypes = new List<string>
            {
                CollectionType.Playlists,
                CollectionType.BoxSets
            };

            var collectionFolder = folder as ICollectionFolder;

            if (collectionFolder == null)
            {
                return false;
            }

            return standaloneTypes.Contains(collectionFolder.CollectionType ?? string.Empty);
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
