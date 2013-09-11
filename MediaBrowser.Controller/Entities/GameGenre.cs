using MediaBrowser.Model.Dto;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace MediaBrowser.Controller.Entities
{
    public class GameGenre : BaseItem, IItemByName
    {
        public GameGenre()
        {
            UserItemCounts = new Dictionary<Guid, ItemByNameCounts>();
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
        public Dictionary<Guid, ItemByNameCounts> UserItemCounts { get; set; }
    }
}
