using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets a single genre
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class GenreHandler : BaseSerializationHandler<IbnItem>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("genre", request);
        }

        protected override Task<IbnItem> GetObjectToSerialize()
        {
            var parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            var user = ApiService.GetUserById(QueryString["userid"], true);

            string name = QueryString["name"];

            return GetGenre(parent, user, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        private async Task<IbnItem> GetGenre(Folder parent, User user, string name)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.Genres != null && item.Genres.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return ApiService.GetIbnItem(await Kernel.Instance.ItemController.GetGenre(name).ConfigureAwait(false), count);
        }
    }
}
