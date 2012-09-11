using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides a handler to set played status for an item
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class PlayedStatusHandler : BaseSerializationHandler<DTOUserItemData>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("PlayedStatus", request);
        }

        protected override Task<DTOUserItemData> GetObjectToSerialize()
        {
            // Get the item
            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            // Get the user
            User user = ApiService.GetUserById(QueryString["userid"], true);

            bool wasPlayed = QueryString["played"] == "1";

            item.SetPlayedStatus(user, wasPlayed);

            UserItemData data = item.GetUserData(user, true);

            return Task.FromResult<DTOUserItemData>(ApiService.GetDTOUserItemData(data));
        }
    }
}