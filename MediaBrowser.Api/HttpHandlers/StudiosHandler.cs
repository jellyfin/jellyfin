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
    public class StudiosHandler : BaseJsonHandler<IEnumerable<IBNItem<Studio>>>
    {
        protected override IEnumerable<IBNItem<Studio>> GetObjectToSerialize()
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
        private IEnumerable<IBNItem<Studio>> GetAllStudios(Folder parent, User user)
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

            // Now go through the dictionary and create a Category for each studio
            List<IBNItem<Studio>> list = new List<IBNItem<Studio>>();

            foreach (string key in data.Keys)
            {
                // Get the original entity so that we can also supply the PrimaryImagePath
                Studio entity = Kernel.Instance.ItemController.GetStudio(key);

                if (entity != null)
                {
                    list.Add(new IBNItem<Studio>()
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
