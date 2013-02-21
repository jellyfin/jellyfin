using System.Collections.Generic;
using MediaBrowser.Model.Dto;
using MediaBrowser.UI.Configuration;
using System;

namespace MediaBrowser.UI.Playback
{
    /// <summary>
    /// Class PlaybackEventArgs
    /// </summary>
    public class PlaybackEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the player.
        /// </summary>
        /// <value>The player.</value>
        public BaseMediaPlayer Player { get; set; }

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public PlayOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the player configuration.
        /// </summary>
        /// <value>The player configuration.</value>
        public PlayerConfiguration PlayerConfiguration { get; set; }
    }

    /// <summary>
    /// Class PlaybackStopEventArgs
    /// </summary>
    public class PlaybackStopEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the player.
        /// </summary>
        /// <value>The player.</value>
        public BaseMediaPlayer Player { get; set; }

        /// <summary>
        /// Gets or sets the items.
        /// </summary>
        /// <value>The items.</value>
        public List<BaseItemDto> Items { get; set; }
    }
}
