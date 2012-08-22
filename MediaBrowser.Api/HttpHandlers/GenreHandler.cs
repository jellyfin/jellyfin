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
    /// <summary>
    /// Gets a single genre
    /// </summary>
    public class GenreHandler : BaseJsonHandler<IBNItem<Genre>>
    {
        protected override Task<IBNItem<Genre>> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            string name = QueryString["name"];

            return GetGenre(parent, user, name);
        }

        /// <summary>
        /// Gets a Genre
        /// </summary>
        private async Task<IBNItem<Genre>> GetGenre(Folder parent, User user, string name)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.Genres != null && item.Genres.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return new IBNItem<Genre>()
            {
                Item = await Kernel.Instance.ItemController.GetGenre(name).ConfigureAwait(false),
                BaseItemCount = count
            };
        }
    }
}
