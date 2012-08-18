using System;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemHandler : BaseJsonHandler<DTOBaseItem>
    {
        protected sealed override DTOBaseItem GetObjectToSerialize()
        {
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            BaseItem item = ItemToSerialize;

            if (item == null)
            {
                return null;
            }

            return ApiService.GetDTOBaseItem(item, user);
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
