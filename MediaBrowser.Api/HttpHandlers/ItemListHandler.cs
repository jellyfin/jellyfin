using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public abstract class ItemListHandler : JsonHandler
    {
        public ItemListHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected sealed override object ObjectToSerialize
        {
            get
            {
                return ItemsToSerialize.Select(i =>
                {
                    return ItemHandler.GetSerializationObject(i, false);

                });
            }
        }

        protected abstract IEnumerable<BaseItem> ItemsToSerialize
        {
            get;
        }
    }
}
