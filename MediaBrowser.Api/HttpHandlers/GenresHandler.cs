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
    public class GenresHandler : BaseJsonHandler<IEnumerable<IBNItem<Genre>>>
    {
        protected override IEnumerable<IBNItem<Genre>> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            return GetAllGenres(parent, user);
        }

        /// <summary>
        /// Gets all genres from all recursive children of a folder
        /// The CategoryInfo class is used to keep track of the number of times each genres appears
        /// </summary>
        public IEnumerable<IBNItem<Genre>> GetAllGenres(Folder parent, User user)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

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

            // Now go through the dictionary and create a Category for each genre
            List<IBNItem<Genre>> list = new List<IBNItem<Genre>>();

            foreach (string key in data.Keys)
            {
                // Get the original entity so that we can also supply the PrimaryImagePath
                Genre entity = Kernel.Instance.ItemController.GetGenre(key);

                if (entity != null)
                {
                    list.Add(new IBNItem<Genre>()
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
