using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides a handler to retrieve a single item
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class ItemHandler : BaseSerializationHandler<DTOBaseItem>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("item", request);
        }
        
        protected override Task<DTOBaseItem> GetObjectToSerialize()
        {
            User user = ApiService.GetUserById(QueryString["userid"], true);

            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            if (item == null)
            {
                return null;
            }

            return ApiService.GetDTOBaseItem(item, user);
        }
    }
}
