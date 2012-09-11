using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.DTO;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// This handler retrieves special features for movies
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class MovieSpecialFeaturesHandler : BaseSerializationHandler<DtoBaseItem[]>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("MovieSpecialFeatures", request);
        }

        protected override Task<DtoBaseItem[]> GetObjectToSerialize()
        {
            User user = ApiService.GetUserById(QueryString["userid"], true);

            var movie = ApiService.GetItemById(ItemId) as Movie;

            // If none
            if (movie.SpecialFeatures == null)
            {
                return Task.FromResult(new DtoBaseItem[] { });
            }

            return Task.WhenAll(movie.SpecialFeatures.Select(i => ApiService.GetDtoBaseItem(i, user, includeChildren: false)));
        }

        protected string ItemId
        {
            get
            {
                return QueryString["id"];
            }
        }
    }
}
