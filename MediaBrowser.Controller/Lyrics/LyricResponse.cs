#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Controller.Lyrics
{
    public class LyricResponse
    {
        public IDictionary<string, object> MetaData { get; set; }

        public IEnumerable<Lyric> Lyrics { get; set; }
    }
}
