using System;
using System.Collections.Generic;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// This is a concrete class that the UI can use to deserialize
    /// It is flat in the sense that it will be used regardless of the type of BaseItem involved
    /// </summary>
    public class ApiBaseItem : BaseItem
    {
    }

    /// <summary>
    /// This is the full return object when requesting an Item
    /// </summary>
    public class ApiBaseItemWrapper<T>
        where T : BaseItem
    {
        public T Item { get; set; }

        public UserItemData UserItemData { get; set; }

        public IEnumerable<ApiBaseItemWrapper<T>> Children { get; set; }

        public bool IsFolder { get; set; }

        public Guid? ParentId { get; set; }

        public string Type { get; set; }

        public bool IsType(Type type)
        {
            return IsType(type.Name);
        }

        public bool IsType(string type)
        {
            return Type.Equals(type, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// If the item does not have a logo, this will hold the Id of the Parent that has one.
        /// </summary>
        public Guid? ParentLogoItemId { get; set; }
    }
}
