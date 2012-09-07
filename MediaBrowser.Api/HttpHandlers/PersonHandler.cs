using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets a single Person
    /// </summary>
    public class PersonHandler : BaseSerializationHandler<IBNItem>
    {
        protected override Task<IBNItem> GetObjectToSerialize()
        {
            Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;
            User user = ApiService.GetUserById(QueryString["userid"], true);

            string name = QueryString["name"];

            return GetPerson(parent, user, name);
        }

        /// <summary>
        /// Gets a Person
        /// </summary>
        private async Task<IBNItem> GetPerson(Folder parent, User user, string name)
        {
            int count = 0;

            // Get all the allowed recursive children
            IEnumerable<BaseItem> allItems = parent.GetParentalAllowedRecursiveChildren(user);

            foreach (var item in allItems)
            {
                if (item.People != null && item.People.ContainsKey(name))
                {
                    count++;
                }
            }

            // Get the original entity so that we can also supply the PrimaryImagePath
            return ApiService.GetIBNItem(await Kernel.Instance.ItemController.GetPerson(name).ConfigureAwait(false), count);
        }
    }
}
