#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Providers.Plugins.Tmdb.Models.General
{
    public class Videos
    {
        public IReadOnlyList<Video> Results { get; set; }
    }
}
