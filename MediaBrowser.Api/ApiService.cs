using System;
using System.Collections.Generic;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api
{
    public static class ApiService
    {
        public static BaseItem GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Kernel.Instance.RootFolder;
            }

            return GetItemById(new Guid(id));
        }

        public static BaseItem GetItemById(Guid id)
        {
            if (id == Guid.Empty)
            {
                return Kernel.Instance.RootFolder;
            }

            return Kernel.Instance.RootFolder.FindById(id);
        }

        public static Person GetPersonByName(string name)
        {
            return null;
        }

        public static IEnumerable<BaseItem> GetItemsWithGenre(Folder parent, string genre)
        {
            return new BaseItem[] { };
        }

        public static IEnumerable<string> GetAllGenres(Folder parent)
        {
            return new string[] { };
        }

        public static IEnumerable<BaseItem> GetRecentlyAddedItems(Folder parent)
        {
            return new BaseItem[] { };
        }

        public static IEnumerable<BaseItem> GetRecentlyAddedUnplayedItems(Folder parent)
        {
            return new BaseItem[] { };
        }

        public static IEnumerable<BaseItem> GetInProgressItems(Folder parent)
        {
            return new BaseItem[] { };
        }
    }
}
