using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class Artist
    /// </summary>
    public class Artist : BaseItem, IItemByName, IHasMusicGenres
    {
        public Artist()
        {
            ItemCounts = new ItemByNameCounts();
            UserItemCounts = new Dictionary<Guid, ItemByNameCounts>();
        }

        public string LastFmImageUrl { get; set; }
        
        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "Artist-" + Name;
        }

        public ItemByNameCounts ItemCounts { get; set; }

        public Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }
    }
}
