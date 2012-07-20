using System.Collections.Generic;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class RecentlyAddedItemsHandler : ItemListHandler
    {
        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                if (QueryString["unplayed"] == "1")
                {
                    return Kernel.Instance.GetRecentlyAddedUnplayedItems(parent, UserId);
                }

                return Kernel.Instance.GetRecentlyAddedItems(parent, UserId);
            }
        }
    }
}
