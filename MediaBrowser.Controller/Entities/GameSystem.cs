using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class GameSystem
    /// </summary>
    public class GameSystem : Folder, IHasLookupInfo<GameSystemInfo>
    {
        /// <summary>
        /// Return the id that should be used to key display prefs for this item.
        /// Default is based on the type for everything except actual generic folders.
        /// </summary>
        /// <value>The display prefs id.</value>
        [IgnoreDataMember]
        public override Guid DisplayPreferencesId => Id;

        [IgnoreDataMember]
        public override bool SupportsPlayedStatus => false;

        [IgnoreDataMember]
        public override bool SupportsInheritedParentImages => false;

        public override double GetDefaultPrimaryImageAspectRatio()
        {
            double value = 16;
            value /= 9;

            return value;
        }

        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystemName { get; set; }

        public override List<string> GetUserDataKeys()
        {
            var list = base.GetUserDataKeys();

            if (!string.IsNullOrEmpty(GameSystemName))
            {
                list.Insert(0, "GameSystem-" + GameSystemName);
            }
            return list;
        }

        protected override bool GetBlockUnratedValue(UserPolicy config)
        {
            // Don't block. Determine by game
            return false;
        }

        public override UnratedItem GetBlockUnratedType()
        {
            return UnratedItem.Game;
        }

        public GameSystemInfo GetLookupInfo()
        {
            var id = GetItemLookupInfo<GameSystemInfo>();

            id.Path = Path;

            return id;
        }

        [IgnoreDataMember]
        public override bool SupportsPeople => false;
    }
}
