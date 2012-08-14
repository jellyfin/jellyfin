using System.Collections.Generic;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets all items within containing a studio
    /// </summary>
    public class ItemsWithYearHandler : ItemListHandler
    {
        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                return Kernel.Instance.GetItemsWithYear(parent, int.Parse(QueryString["name"]), UserId);
            }
        }
    }
}
