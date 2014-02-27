using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class LinkedChild
    {
        public string Path { get; set; }
        public LinkedChildType Type { get; set; }

        /// <summary>
        /// Serves as a cache
        /// </summary>
        [IgnoreDataMember]
        public Guid? ItemId { get; set; }
    }

    public enum LinkedChildType
    {
        Manual = 1,
        Shortcut = 2
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
            return (obj.Path + obj.Type.ToString()).GetHashCode();
        }
    }
}
