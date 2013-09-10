using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Genre
    /// </summary>
    public class Genre : BaseItem, IItemByName
    {
        public Genre()
        {
            ItemCounts = new ItemByNameCounts();
            UserItemCounts = new Dictionary<Guid, ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Genre-" + Name;
        }

        public ItemByNameCounts ItemCounts { get; set; }

        public Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }
    }
}
