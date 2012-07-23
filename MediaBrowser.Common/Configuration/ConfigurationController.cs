using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MediaBrowser.Common.Json;

namespace MediaBrowser.Common.Configuration
{
    public class ConfigurationController<TConfigurationType>
        where TConfigurationType : BaseConfiguration, new ()
    {
        /// <summary>
        /// The path to the configuration file
        /// </summary>
        public string Path { get; set; }

        public TConfigurationType Configuration { get; set; }

        public void Reload()
        {
            if (!File.Exists(Path))
            {
                Configuration = new TConfigurationType();
            }
            else
            {
                Configuration = JsonSerializer.DeserializeFromFile<TConfigurationType>(Path);
            }
        }

        public void Save()
        {
        }
    }
}
