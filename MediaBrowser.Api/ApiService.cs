using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Api
{
    /// <summary>
    /// Contains some helpers for the api
    /// </summary>
    public static class ApiService
    {
        public static BaseItem GetItemById(string id)
        {
            Guid guid = string.IsNullOrEmpty(id) ? Guid.Empty : new Guid(id);

            return Kernel.Instance.GetItemById(guid);
        }

        /// <summary>
        /// Takes a BaseItem and returns the actual object that will be serialized by the api
        /// </summary>
        public static ApiBaseItemWrapper<BaseItem> GetSerializationObject(BaseItem item, bool includeChildren, Guid userId)
        {
            ApiBaseItemWrapper<BaseItem> wrapper = new ApiBaseItemWrapper<BaseItem>()
            {
                Item = item,
                UserItemData = Kernel.Instance.GetUserItemData(userId, item.Id),
                ItemType = item.GetType()
            };

            if (includeChildren)
            {
                var folder = item as Folder;

                if (folder != null)
                {
                    wrapper.Children = Kernel.Instance.GetParentalAllowedChildren(folder, userId).Select(c => GetSerializationObject(c, false, userId));
                }
            }

            return wrapper;
        }
    }
}
