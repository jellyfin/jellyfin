using System.Collections.Generic;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class InProgressItemsHandler : ItemListHandler
    {
        public InProgressItemsHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                return ApiService.GetInProgressItems(parent);
            }
        }
    }
}
