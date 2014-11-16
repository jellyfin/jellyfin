using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    [Obsolete]
    public class AdultVideo : Video, IHasProductionLocations, IHasTaglines
    {
        public List<string> ProductionLocations { get; set; }

        public List<string> Taglines { get; set; }

        public AdultVideo()
        {
            Taglines = new List<string>();
            ProductionLocations = new List<string>();
        }
    }
}
