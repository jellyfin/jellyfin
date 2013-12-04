using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Marker interface
    /// </summary>
    public interface IItemByName
    {
        List<ItemByNameCounts> UserItemCountList { get; set; }
    }

    public interface IHasDualAccess : IItemByName
    {
        bool IsAccessedByName { get; }
    }

    public static class ItemByNameExtensions
    {
        public static ItemByNameCounts GetItemByNameCounts(this IItemByName item, Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new ArgumentNullException("userId");
            }

            return item.UserItemCountList.FirstOrDefault(i => i.UserId == userId);
        }

        public static void SetItemByNameCounts(this IItemByName item, Guid userId, ItemByNameCounts counts)
        {
            var current = item.UserItemCountList.FirstOrDefault(i => i.UserId == userId);

            if (current != null)
            {
                item.UserItemCountList.Remove(current);
            }

            counts.UserId = userId;
            item.UserItemCountList.Add(counts);
        }
    }
}
