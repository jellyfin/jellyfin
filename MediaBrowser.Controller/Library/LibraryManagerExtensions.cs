using System;
using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    public static class LibraryManagerExtensions
    {
        public static BaseItem GetItemById(this ILibraryManager manager, string id)
        {
            return manager.GetItemById(new Guid(id));
        }
    }
}