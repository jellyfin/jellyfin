using System.Collections.Generic;
using System.Runtime.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Movies.Entities
{
    public class Movie : Video
    {
        public string TmdbId { get; set; }
        public string ImdbId { get; set; }

        [IgnoreDataMember]
        public IEnumerable<Video> SpecialFeatures { get; set; }
    }
}
