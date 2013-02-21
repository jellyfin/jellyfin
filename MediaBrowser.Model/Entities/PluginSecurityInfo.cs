using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class PluginSecurityInfo
    /// </summary>
    [ProtoContract]
    public class PluginSecurityInfo
    {
        /// <summary>
        /// Gets or sets the supporter key.
        /// </summary>
        /// <value>The supporter key.</value>
        [ProtoMember(1)]
        public string SupporterKey { get; set; }

        /// <summary>
        /// Gets or sets the legacy supporter key.
        /// </summary>
        /// <value><c>The legacy supporter key</value>
        [ProtoMember(2)]
        public string LegacyKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is MB supporter.
        /// </summary>
        /// <value><c>true</c> if this instance is MB supporter; otherwise, <c>false</c>.</value>
        [ProtoMember(3)]
        public bool IsMBSupporter { get; set; }
    }
}
