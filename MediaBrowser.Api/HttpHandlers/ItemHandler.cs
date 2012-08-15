using System;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemHandler : BaseJsonHandler
    {
        protected sealed override object GetObjectToSerialize()
        {
            Guid userId = Guid.Parse(QueryString["userid"]);

            BaseItem item = ItemToSerialize;

            if (item == null)
            {
                return null;
            }

            return ApiService.GetSerializationObject(item, true, userId);
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
