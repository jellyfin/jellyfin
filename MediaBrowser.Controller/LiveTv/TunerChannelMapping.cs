using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.LiveTv
{
    public class TunerChannelMapping
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public string ProviderChannelNumber { get; set; }
        public string ProviderChannelName { get; set; }
    }
}
