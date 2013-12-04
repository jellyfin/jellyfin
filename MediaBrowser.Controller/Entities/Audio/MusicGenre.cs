using System.Runtime.Serialization;
using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.Audio
{
    /// <summary>
    /// Class MusicGenre
    /// </summary>
    public class MusicGenre : BaseItem, IItemByName
    {
        public MusicGenre()
        {
            UserItemCountList = new List<ItemByNameCounts>();
        }

        /// <summary>
        /// Gets the user data key.
        /// </summary>
        /// <returns>System.String.</returns>
        public override string GetUserDataKey()
        {
            return "MusicGenre-" + Name;
        }

        [IgnoreDataMember]
        public List<ItemByNameCounts> UserItemCountList { get; set; }
    }
}
