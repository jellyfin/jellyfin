using MediaBrowser.Model.Dto;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class GameGenre : BaseItem, IItemByName
    {
        public GameGenre()
        {
            UserItemCountList = new List<ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "GameGenre-" + Name;
        }

        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }

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
