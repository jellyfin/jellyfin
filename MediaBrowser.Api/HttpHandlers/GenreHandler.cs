using System.Collections.Generic;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    /// <summary>
    /// Gets all items within a Genre
    /// </summary>
    public class GenreHandler : ItemListHandler
    {
        protected override IEnumerable<BaseItem> ItemsToSerialize
        {
            get
            {
                Folder parent = ApiService.GetItemById(QueryString["id"]) as Folder;

                return Kernel.Instance.GetItemsWithGenre(parent, QueryString["name"], UserId);
            }
        }
    }
}
