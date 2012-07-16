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
            Guid guid = string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);

            return Kernel.Instance.GetItemById(guid);
        }

        public static IEnumerable<CategoryInfo> GetAllStudios(Folder parent, Guid userId)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();
            
            IEnumerable<BaseItem> allItems = Kernel.Instance.GetParentalAllowedRecursiveChildren(parent, userId);

            foreach (var item in allItems)
            {
                if (item.Studios == null)
                {
                    continue;
                }

                foreach (string val in item.Studios)
                {
                    if (!data.ContainsKey(val))
                    {
                        data.Add(val, 1);
                    }
                    else
                    {
                        data[val]++;
                    }
                }
            }

            List<CategoryInfo> list = new List<CategoryInfo>();

            foreach (string key in data.Keys)
            {
                list.Add(new CategoryInfo()
                {
                    Name = key,
                    ItemCount = data[key]

                });
            }
            
            return list;
        }

        public static IEnumerable<CategoryInfo> GetAllGenres(Folder parent, Guid userId)
        {
            Dictionary<string, int> data = new Dictionary<string, int>();

            IEnumerable<BaseItem> allItems = Kernel.Instance.GetParentalAllowedRecursiveChildren(parent, userId);

            foreach (var item in allItems)
            {
                if (item.Genres == null)
                {
                    continue;
                }

                foreach (string val in item.Genres)
                {
                    if (!data.ContainsKey(val))
                    {
                        data.Add(val, 1);
                    }
                    else
                    {
                        data[val]++;
                    }
                }
            }

            List<CategoryInfo> list = new List<CategoryInfo>();

            foreach (string key in data.Keys)
            {
                list.Add(new CategoryInfo()
                {
                    Name = key,
                    ItemCount = data[key]

                });
            }

            return list;
        }
    }
}
