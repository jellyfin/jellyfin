using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="HttpHeaderInfo" />.
    /// </summary>
    public class HttpHeaderInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [XmlAttribute("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [XmlAttribute("value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the header match type.
        /// </summary>
        [XmlAttribute("match")]
        public HeaderMatchType Match { get; set; }
    }
}
