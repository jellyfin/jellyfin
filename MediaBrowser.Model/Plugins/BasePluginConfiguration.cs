using System;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Plugins
{
    public class BasePluginConfiguration
    {
        public bool Enabled { get; set; }

        [IgnoreDataMember]
        public DateTime DateLastModified { get; set; }

        public BasePluginConfiguration()
        {
            Enabled = true;
        }
    }
}
