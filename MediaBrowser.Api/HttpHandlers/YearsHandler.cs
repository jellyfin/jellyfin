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
    public class YearsHandler : BaseJsonHandler<IEnumerable<IBNItem<Year>>>
    {
        protected override Task<IEnumerable<IBNItem<Year>>> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            return GetAllYears(parent, user);
        }

        /// <summary>
        /// Gets all years from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each year appears
        /// </summary>
        private async Task<IEnumerable<IBNItem<Year>>> GetAllYears(Folder parent, User user)
        {
            Dictionary<int, int> data = new Dictionary<int, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                // Add the year from the item to the data dictionary
                // If the year already exists, increment the count
                if (item.ProductionYear == null)
                {
                    continue;
                }

                if (!data.ContainsKey(item.ProductionYear.Value))
                {
                    data.Add(item.ProductionYear.Value, 1);
                }
                else
                {
                    data[item.ProductionYear.Value]++;
                }
            }

            IEnumerable<Year> entities = await Task.WhenAll<Year>(data.Keys.Select(key => { return Kernel.Instance.ItemController.GetYear(key); })).ConfigureAwait(false);

            return entities.Select(e => new IBNItem<Year>() { Item = e, BaseItemCount = data[int.Parse(e.Name)] });
        }
    }
}
