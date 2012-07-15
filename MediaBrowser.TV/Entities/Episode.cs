using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.TV.Entities
{
    public class Episode : Video
    {
        public string SeasonNumber { get; set; }
        public string EpisodeNumber { get; set; }
        public DateTime? FirstAired { get; set; }
    }
}
