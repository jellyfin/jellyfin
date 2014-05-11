using System;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    public static class LibraryManagerExtensions
    {
        public static Task DeleteItem(this ILibraryManager manager, BaseItem item)
        {
            return manager.DeleteItem(item, new DeleteOptions
            {
                DeleteFileLocation = true
            });
        }

        public static BaseItem GetItemById(this ILibraryManager manager, string id)
        {
            return manager.GetItemById(new Guid(id));
        }
    }
}