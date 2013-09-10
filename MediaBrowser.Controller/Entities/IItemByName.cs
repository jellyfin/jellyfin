using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IItemByName
    {
        ItemByNameCounts ItemCounts { get; set; }

        Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }
    }

    public static class IItemByNameExtensions
    {
        public static ItemByNameCounts GetItemByNameCounts(this IItemByName item, User user)
        {
            if (user == null)
            {
                return item.ItemCounts;
            }

            ItemByNameCounts counts;

            if (item.UserItemCounts.TryGetValue(user.Id, out counts))
            {
                return counts;
            }

            return null;
        }
    }
}
