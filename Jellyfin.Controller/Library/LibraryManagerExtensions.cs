using System;
using Jellyfin.Controller.Entities;

namespace Jellyfin.Controller.Library
{
    public static class LibraryManagerExtensions
    {
        public static BaseItem GetItemById(this ILibraryManager manager, string id)
        {
            return manager.GetItemById(new Guid(id));
        }
    }
}
