using System;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Simple episode structure used by MissingEpisodeProvider
    /// </summary>
    public class MissingEpisodeInfo
    {
        /// <summary>
        /// Gets or sets the episode's season index number.
        /// </summary>
        public int SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the episode index number.
        /// </summary>
        public int EpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the episode premiere date.
        /// </summary>
        public DateTime AirDate { get; set; }
    }
}
