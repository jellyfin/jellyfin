using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;

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
            Folder folder = item as Folder;

            if (includeChildren && folder != null)
            {
                return new
                {
                    BaseItem = item,
                    Children = folder.Children,
                    Type = item.GetType().Name
                };
            }
            else
            {
                return new
                {
                    BaseItem = item,
                    Type = item.GetType().Name
                };
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
