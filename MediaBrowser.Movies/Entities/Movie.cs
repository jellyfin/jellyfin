using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Movies.Entities
{
    public class Movie : Video
    {
        public IEnumerable<Video> SpecialFeatures { get; set; }
    }
}
