using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Movies.Entities
{
    public class Movie : Video
    {
        public string TmdbId { get; set; }
        public string ImdbId { get; set; }

        public IEnumerable<Video> SpecialFeatures { get; set; }
    }
}
