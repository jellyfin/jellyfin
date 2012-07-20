using System;
using MediaBrowser.Net.Handlers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemHandler : JsonHandler
    {
        protected sealed override object ObjectToSerialize
        {
            get
            {
                Guid userId = Guid.Parse(QueryString["userid"]);

                return ApiService.GetSerializationObject(ItemToSerialize, true, userId);
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
