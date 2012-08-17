using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Api.HttpHandlers
{
    public class YearsHandler : BaseJsonHandler<IEnumerable<IBNItem<Year>>>
    {
        protected override IEnumerable<IBNItem<Year>> GetObjectToSerialize()
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
        private IEnumerable<IBNItem<Year>> GetAllYears(Folder parent, User user)
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

            // Now go through the dictionary and create a Category for each studio
            List<IBNItem<Year>> list = new List<IBNItem<Year>>();

            foreach (int key in data.Keys)
            {
                // Get the original entity so that we can also supply the PrimaryImagePath
                Year entity = Kernel.Instance.ItemController.GetYear(key);

                if (entity != null)
                {
                    list.Add(new IBNItem<Year>()
                    {
                        Item = entity,
                        BaseItemCount = data[key]
                    });
                }
            }

            return list;
        }
    }
}
