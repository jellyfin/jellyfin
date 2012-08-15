using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.TV.Entities
{
    public class Series : Folder
    {
        public string Status { get; set; }
        public IEnumerable<DayOfWeek> AirDays { get; set; }
        public string AirTime { get; set; }
    }
}
