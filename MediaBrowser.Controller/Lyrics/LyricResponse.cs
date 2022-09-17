#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Lyrics
{
    /// <summary>
    /// LyricResponse model.
    /// </summary>
    public class LyricResponse
    {
        /// <summary>
        /// Gets or sets MetaData.
        /// </summary>
        public IDictionary<string, object> MetaData { get; set; }

        /// <summary>
        /// Gets or sets Lyrics.
        /// </summary>
        public IEnumerable<Lyric> Lyrics { get; set; }
    }
}
