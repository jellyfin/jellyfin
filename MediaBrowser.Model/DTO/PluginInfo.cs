using System;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is a serializable stub class that is used by the api to provide information about installed plugins.
    /// </summary>
    public class PluginInfo
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool DownloadToUI { get; set; }
        public DateTime ConfigurationDateLastModified { get; set; }
        public Version Version { get; set; }
    }
}
