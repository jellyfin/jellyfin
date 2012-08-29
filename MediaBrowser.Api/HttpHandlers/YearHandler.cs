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
    /// Gets a single year
    /// </summary>
    public class YearHandler : BaseJsonHandler<IBNItem>
    {
        protected override Task<IBNItem> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            Guid userId = Guid.Parse(QueryString["userid"]);
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            string year = QueryString["year"];

            return GetYear(parent, user, int.Parse(year));
        }

        /// <summary>
        /// Gets a Year
        /// </summary>
        private async Task<IBNItem> GetYear(Folder parent, User user, int year)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.ProductionYear.HasValue && item.ProductionYear.Value == year)
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return ApiService.GetIBNItem(await Kernel.Instance.ItemController.GetYear(year).ConfigureAwait(false), count);
        }
    }
}
