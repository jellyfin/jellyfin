using System.Collections.Generic;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class InProgressItemsHandler : ItemListHandler
    {
        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                return Kernel.Instance.GetInProgressItems(parent, UserId);
            }
        }
    }
}
