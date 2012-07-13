using System.Collections.Generic;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Entities;
using MediaBrowser.Api;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets all items within a Genre
    /// </summary>
    public class GenreHandler : ItemListHandler
    {
        public GenreHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                return ApiService.GetItemsWithGenre(parent, QueryString["name"]);
            }
        }
    }
}
