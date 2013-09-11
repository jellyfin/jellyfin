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
            UserItemCounts = new Dictionary<Guid, ItemByNameCounts>();
        }

        [IgnoreDataMember]
        public Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Year-" + Name;
        }
    }
}
