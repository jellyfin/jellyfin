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
    /// Gets a single studio
    /// </summary>
    public class StudioHandler : BaseJsonHandler<IBNItem>
    {
        protected override Task<IBNItem> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            string name = QueryString["name"];

            return GetStudio(parent, user, name);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        private async Task<IBNItem> GetStudio(Folder parent, User user, string name)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.Studios != null && item.Studios.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return ApiService.GetIBNItem(await Kernel.Instance.ItemController.GetStudio(name).ConfigureAwait(false), count);
        }
    }
}
