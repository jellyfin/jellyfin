using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets a single year
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class YearHandler : BaseSerializationHandler<IBNItem>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("year", request);
        }
        
        protected override Task<IBNItem> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            User user = ApiService.GetUserById(QueryString["userid"], true);

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
