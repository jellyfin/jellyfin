using MediaBrowser.Model.Entities;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MediaBrowser.TV.Entities
{
    public class Series : Folder
    {
        public string TvdbId { get; set; }
        public string Status { get; set; }
        public IEnumerable<DayOfWeek> AirDays { get; set; }
        public string AirTime { get; set; }
    }
}
