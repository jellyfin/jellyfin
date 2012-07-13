using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class GenresHandler : JsonHandler
    {
        public GenresHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected sealed override object ObjectToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                return ApiService.GetAllGenres(parent);
            }
        }
    }
}
