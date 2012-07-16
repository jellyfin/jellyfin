using System.Collections.Generic;
using MediaBrowser.Model.Entities;
using System.Runtime.Serialization;

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
