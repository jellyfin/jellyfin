using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Provides a handler to set a user's rating for an item
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class UserItemRatingHandler : BaseSerializationHandler<DTOUserItemData>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("UserItemRating", request);
        }

        protected override Task<DTOUserItemData> GetObjectToSerialize()
        {
            // Get the item
            BaseItem item = ApiService.GetItemById(QueryString["id"]);

            // Get the user
            User user = ApiService.GetUserById(QueryString["userid"], true);

            // Get the user data for this item
            UserItemData data = item.GetUserData(user, true);

            // If clearing the rating, set it to null
            if (QueryString["clear"] == "1")
            {
                data.Rating = null;
            }

            else
            {
                data.Likes = QueryString["likes"] == "1";
            }

            return Task.FromResult<DTOUserItemData>(ApiService.GetDTOUserItemData(data));
        }
    }
}