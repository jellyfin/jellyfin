using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities.TV
{
    public class Series : Folder
    {
        public string Status { get; set; }
        public IEnumerable<DayOfWeek> AirDays { get; set; }
        public string AirTime { get; set; }
    }
}
