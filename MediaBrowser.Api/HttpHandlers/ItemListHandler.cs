using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public abstract class ItemListHandler : BaseJsonHandler<IEnumerable<BaseItemWrapper<BaseItem>>>
    {
        protected override IEnumerable<BaseItemWrapper<BaseItem>> GetObjectToSerialize()
        {
            return ItemsToSerialize.Select(i =>
            {
                return ApiService.GetSerializationObject(i, false, UserId);

            });
        }

        protected abstract IEnumerable<BaseItem> ItemsToSerialize
        {
            get;
        }

        protected Guid UserId
        {
            get
            {
                return Guid.Parse(QueryString["userid"]);
            }
        }
    }
}
