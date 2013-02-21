using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides a handler to retrieve a single item
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class ItemHandler : BaseSerializationHandler<DtoBaseItem>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("item", request);
        }
        
        protected override Task<DtoBaseItem> GetObjectToSerialize()
        {
            User user = ApiService.GetUserById(QueryString["userid"], true);

            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            if (item == null)
            {
                return null;
            }

            return ApiService.GetDtoBaseItem(item, user);
        }
    }
}
