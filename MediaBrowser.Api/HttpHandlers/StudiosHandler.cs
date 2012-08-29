using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    public class StudiosHandler : BaseJsonHandler<IEnumerable<IBNItem>>
    {
        protected override Task<IEnumerable<IBNItem>> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            return GetAllStudios(parent, user);
        }

        /// <summary>
        /// Gets all studios from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each studio appears
        /// </summary>
        private async Task<IEnumerable<IBNItem>> GetAllStudios(Folder parent, User user)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

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

            IEnumerable<Studio> entities = await Task.WhenAll<Studio>(data.Keys.Select(key => { return Kernel.Instance.ItemController.GetStudio(key); })).ConfigureAwait(false);

            return entities.Select(e => ApiService.GetIBNItem(e, data[e.Name]));
        }
    }
}
