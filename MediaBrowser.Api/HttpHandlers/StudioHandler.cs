using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets a single studio
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class StudioHandler : BaseSerializationHandler<IbnItem>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("studio", request);
        }

        protected override Task<IbnItem> GetObjectToSerialize()
        {
            var parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            var user = ApiService.GetUserById(QueryString["userid"], true);

            string name = QueryString["name"];

            return GetStudio(parent, user, name);
        }

        /// <summary>
        /// Gets a Studio
        /// </summary>
        private async Task<IbnItem> GetStudio(Folder parent, User user, string name)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.Studios != null && item.Studios.Any(s => s.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return ApiService.GetIbnItem(await Kernel.Instance.ItemController.GetStudio(name).ConfigureAwait(false), count);
        }
    }
}
