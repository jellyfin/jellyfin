using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public string Path { get; set; }
        public LinkedChildType Type { get; set; }

        public string ItemName { get; set; }
        public string ItemType { get; set; }
        public int? ItemYear { get; set; }
        public int? ItemIndexNumber { get; set; }

        /// <summary>
        /// Serves as a cache
        /// </summary>
        [IgnoreDataMember]
        public Guid? ItemId { get; set; }

        public static LinkedChild Create(BaseItem item)
        {
            return new LinkedChild
            {
                ItemName = item.Name,
                ItemYear = item.ProductionYear,
                ItemType = item.GetType().Name,
                Type = LinkedChildType.Manual,
                ItemIndexNumber = item.IndexNumber
            };
        }
    }

    public enum LinkedChildType
    {
        Manual = 0,
        Shortcut = 1
    }

    public class LinkedChildComparer : IEqualityComparer<LinkedChild>
    {
        public bool Equals(LinkedChild x, LinkedChild y)
        {
            if (x.Type == y.Type)
            {
                return string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public int GetHashCode(LinkedChild obj)
        {
            return (obj.Path + obj.Type).GetHashCode();
        }
    }
}
