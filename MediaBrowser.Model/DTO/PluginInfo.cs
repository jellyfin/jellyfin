using System;
using ProtoBuf;

namespace MediaBrowser.Model.DTO
{
    /// <summary>
    /// This is a serializable stub class that is used by the api to provide information about installed plugins.
    /// </summary>
    [ProtoContract]
    public class PluginInfo
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Path { get; set; }

        [ProtoMember(3)]
        public bool Enabled { get; set; }

        [ProtoMember(4)]
        public bool DownloadToUI { get; set; }

        [ProtoMember(5)]
        public DateTime ConfigurationDateLastModified { get; set; }

        [ProtoMember(6)]
        public Version Version { get; set; }
    }
}
