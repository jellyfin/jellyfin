using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Common.Json;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemHandler : JsonHandler
    {
        public ItemHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected sealed override object ObjectToSerialize
        {
            get
            {
                return GetSerializationObject(ItemToSerialize, true);
            }
        }

        public static object GetSerializationObject(BaseItem item, bool includeChildren)
        {
            if (includeChildren && item.IsFolder)
            {
                Folder folder = item as Folder;

                return new
                {
                    BaseItem = item,
                    Children = folder.Children
                };
            }
            else
            {
                return item;
            }
        }

        protected virtual BaseItem ItemToSerialize
        {
            get
            {
                return ApiService.GetItemById(QueryString["id"]);
            }
        }
    }
}
