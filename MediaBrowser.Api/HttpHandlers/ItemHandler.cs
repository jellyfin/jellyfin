using System;
using MediaBrowser.Api.Model;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
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
                Guid userId = Guid.Parse(QueryString["userid"]);

                return GetSerializationObject(ItemToSerialize, true, userId);
            }
        }

        public static object GetSerializationObject(BaseItem item, bool includeChildren, Guid userId)
        {
            BaseItemInfo wrapper = new BaseItemInfo()
            {
                Item = item,
                UserItemData = Kernel.Instance.GetUserItemData(userId, item.Id)
            };

            if (includeChildren)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    wrapper.Children = Kernel.Instance.GetParentalAllowedChildren(folder, userId);
                }
            }

            return wrapper;
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
