using MediaBrowser.Controller.TV;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Querying;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Entities
{
    public class UserView : Folder
    {
        public string ViewType { get; set; }
        public Guid ParentId { get; set; }

        public Guid? UserId { get; set; }

        public static ITVSeriesManager TVSeriesManager;

        public override Task<QueryResult<BaseItem>> GetItems(InternalItemsQuery query)
        {
            var parent = this as Folder;

            if (ParentId != Guid.Empty)
            {
                parent = LibraryManager.GetItemById(ParentId) as Folder ?? parent;
            }

            return new UserViewBuilder(UserViewManager, LiveTvManager, ChannelManager, LibraryManager, Logger, UserDataManager, TVSeriesManager, CollectionManager)
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

        public override IEnumerable<BaseItem> GetRecursiveChildren(User user, bool includeLinkedChildren = true)
        {
            var result = GetItems(new InternalItemsQuery
            {
                User = user,
                Recursive = true

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
                CollectionType.Trailers
            };

            var collectionFolder = folder as ICollectionFolder;

            if (collectionFolder == null)
            {
                return false;
            }

            return standaloneTypes.Contains(collectionFolder.CollectionType ?? string.Empty);
        }
    }
}
