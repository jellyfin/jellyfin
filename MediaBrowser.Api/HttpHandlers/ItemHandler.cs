using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    public class ItemHandler : BaseSerializationHandler<DTOBaseItem>
    {
        protected override Task<DTOBaseItem> GetObjectToSerialize()
        {
            User user = ApiService.GetUserById(QueryString["userid"], true);

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
