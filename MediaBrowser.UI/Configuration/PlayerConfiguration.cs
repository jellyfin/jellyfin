using MediaBrowser.Model.Entities;

namespace MediaBrowser.UI.Configuration
{
    /// <summary>
    /// Class PlayerConfiguration
    /// </summary>
    public class PlayerConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the player.
        /// </summary>
        /// <value>The name of the player.</value>
        public string PlayerName { get; set; }

        /// <summary>
        /// Gets or sets the item types.
        /// </summary>
        /// <value>The item types.</value>
        public string[] ItemTypes { get; set; }

        /// <summary>
        /// Gets or sets the file extensions.
        /// </summary>
        /// <value>The file extensions.</value>
        public string[] FileExtensions { get; set; }

        /// <summary>
        /// Gets or sets the video types.
        /// </summary>
        /// <value>The video types.</value>
        public VideoType[] VideoTypes { get; set; }
        
        /// <summary>
        /// Gets or sets the video formats.
        /// </summary>
        /// <value>The video formats.</value>
        public VideoFormat[] VideoFormats { get; set; }

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        /// <value>The command.</value>
        public string Command { get; set; }

        /// <summary>
        /// Gets or sets the args.
        /// </summary>
        /// <value>The args.</value>
        public string Args { get; set; }
    }
}
