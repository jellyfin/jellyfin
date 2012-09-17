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
    public class StudiosHandler : BaseSerializationHandler<IbnItem[]>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("studios", request);
        }

        protected override Task<IbnItem[]> GetObjectToSerialize()
        {
            var parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            var user = ApiService.GetUserById(QueryString["userid"], true);

            return GetAllStudios(parent, user);
        }

        /// <summary>
        /// Gets all studios from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each studio appears
        /// </summary>
        private async Task<IbnItem[]> GetAllStudios(Folder parent, User user)
        {
            var data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add each studio from the item to the data dictionary
                // If the studio already exists, increment the count
                if (item.Studios == null)
                {
                    continue;
                }

                foreach (string val in item.Studios)
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

            // Get the Studio objects
            Studio[] entities = await Task.WhenAll(data.Keys.Select(key => Kernel.Instance.ItemController.GetStudio(key))).ConfigureAwait(false);

            // Convert to an array of IBNItem
            var items = new IbnItem[entities.Length];

            for (int i = 0; i < entities.Length; i++)
            {
                Studio e = entities[i];

                items[i] = ApiService.GetIbnItem(e, data[e.Name]);
            }

            return items;
        }
    }
}
