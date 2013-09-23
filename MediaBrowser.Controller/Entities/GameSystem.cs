using System;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Class GameSystem
    /// </summary>
    public class GameSystem : Folder
    {
        /// <summary>
        /// Return the id that should be used to key display prefs for this item.
        /// Default is based on the type for everything except actual generic folders.
        /// </summary>
        /// <value>The display prefs id.</value>
        public override Guid DisplayPreferencesId
        {
            get
            {
                return Id;
            }
        }

        /// <summary>
        /// Gets or sets the game system.
        /// </summary>
        /// <value>The game system.</value>
        public string GameSystemName { get; set; }
    }
}
