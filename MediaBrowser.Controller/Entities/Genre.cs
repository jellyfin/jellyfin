using MediaBrowser.Model.Dto;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class Genre
    /// </summary>
    public class Genre : BaseItem, IItemByName
    {
        public Genre()
        {
            UserItemCountList = new List<ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Genre-" + Name;
        }

        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }
    }
}
