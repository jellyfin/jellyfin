using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.TV.Entities
{
    public class Series : Folder
    {
        public string TvdbId { get; set; }
        public string Status { get; set; }
        public string AirDay { get; set; }
        public string AirTime { get; set; }
    }
}
