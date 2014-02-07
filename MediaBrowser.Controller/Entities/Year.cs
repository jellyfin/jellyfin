using System.Runtime.Serialization;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Year
    /// </summary>
    public class Year : BaseItem, IItemByName
    {
        public Year()
        {
            UserItemCountList = new List<ItemByNameCounts>();
        }

        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Year-" + Name;
        }

        /// <summary>
        /// Returns the folder containing the item.
        /// If the item is a folder, it returns the folder itself
        /// </summary>
        /// <value>The containing folder path.</value>
        public override string ContainingFolderPath
        {
            get
            {
                return Path;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is owned item.
        /// </summary>
        /// <value><c>true</c> if this instance is owned item; otherwise, <c>false</c>.</value>
        public override bool IsOwnedItem
        {
            get
            {
                return false;
            }
        }
    }
}
