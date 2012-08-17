using System.Collections.Generic;
using System.Runtime.Serialization;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Movies.Entities
{
    public class Movie : Video
    {
        [IgnoreDataMember]
        public IEnumerable<Video> SpecialFeatures { get; set; }
    }
}
