using System;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class GameSystemSummary
    /// </summary>
    public class GameSystemSummary
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Gets or sets the game count.
        /// </summary>
        /// <value>The game count.</value>
        public int GameCount { get; set; }

        /// <summary>
        /// Gets or sets the game extensions.
        /// </summary>
        /// <value>The game extensions.</value>
        public string[] GameFileExtensions { get; set; }

        /// <summary>
        /// Gets or sets the client installed game count.
        /// </summary>
        /// <value>The client installed game count.</value>
        public int ClientInstalledGameCount { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameSystemSummary"/> class.
        /// </summary>
        public GameSystemSummary()
        {
            GameFileExtensions = Array.Empty<string>();
        }
    }
}
