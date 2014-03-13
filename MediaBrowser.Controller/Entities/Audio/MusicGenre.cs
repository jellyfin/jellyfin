using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicGenre
    /// </summary>
    public class MusicGenre : BaseItem, IItemByName
    {
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "MusicGenre-" + Name;
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

        public IEnumerable<BaseItem> GetTaggedItems(IEnumerable<BaseItem> inputItems)
        {
            return inputItems.Where(i => (i is IHasMusicGenres) && i.Genres.Contains(Name, StringComparer.OrdinalIgnoreCase));
        }
    }
}
