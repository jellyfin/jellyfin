using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets a single Person
    /// </summary>
    [Export(typeof(BaseHandler))]
    public class PersonHandler : BaseSerializationHandler<IbnItem>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("person", request);
        }

        protected override Task<IbnItem> GetObjectToSerialize()
        {
            var parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            var user = ApiService.GetUserById(QueryString["userid"], true);

            string name = QueryString["name"];

            return GetPerson(parent, user, name);
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        private async Task<IbnItem> GetPerson(Folder parent, User user, string name)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.People != null && item.People.ContainsKey(name))
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return ApiService.GetIbnItem(await Kernel.Instance.ItemController.GetPerson(name).ConfigureAwait(false), count);
        }
    }
}
