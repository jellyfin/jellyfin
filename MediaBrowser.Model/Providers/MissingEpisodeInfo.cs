using System;

namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Simple episode structure used by MissingEpisodeProvider
    /// </summary>
    public class MissingEpisodeInfo
    {
        /// <summary>
        /// Episode's season index number.
        /// </summary>
        public int seasonNumber { get; set; }

        /// <summary>
        /// Episode index number.
        /// </summary>
        public int episodeNumber { get; set; }

        /// <summary>
        /// Episode premiere date.
        /// </summary>
        public DateTime airDate { get; set; }
    }
}
