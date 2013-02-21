using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    public class GenresHandler : BaseSerializationHandler<IbnItem[]>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("genres", request);
        }

        protected override Task<IbnItem[]> GetObjectToSerialize()
        {
            var parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            User user = ApiService.GetUserById(QueryString["userid"], true);

            return GetAllGenres(parent, user);
        }

        /// <summary>
        /// Gets all genres from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each genres appears
        /// </summary>
        private async Task<IbnItem[]> GetAllGenres(Folder parent, User user)
        {
            var data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add each genre from the item to the data dictionary
                // If the genre already exists, increment the count
                if (item.Genres == null)
                {
                    continue;
                }

                foreach (string val in item.Genres)
                {
                    if (!data.ContainsKey(val))
                    {
                        data.Add(val, 1);
                    }
                    else
                    {
                        data[val]++;
                    }
                }
            }

            // Get the Genre objects
            Genre[] entities = await Task.WhenAll(data.Keys.Select(key => Kernel.Instance.ItemController.GetGenre(key))).ConfigureAwait(false);

            // Convert to an array of IBNItem
            var items = new IbnItem[entities.Length];

            for (int i = 0; i < entities.Length; i++)
            {
                Genre e = entities[i];

                items[i] = ApiService.GetIbnItem(e, data[e.Name]);
            }

            return items;
        }
    }
}
