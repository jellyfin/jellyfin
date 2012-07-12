using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Plugins
{
    public class BasePluginConfiguration
    {
        public bool Enabled { get; set; }

        public BasePluginConfiguration()
        {
            Enabled = true;
        }
    }
}
