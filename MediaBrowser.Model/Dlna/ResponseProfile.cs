#nullable disable
#pragma warning disable CS1591

using System;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    public class ResponseProfile
    {
        public ResponseProfile()
        {
            Conditions = Array.Empty<ProfileCondition>();
        }

        [XmlAttribute("container")]
        public string Container { get; set; }

        [XmlAttribute("audioCodec")]
        public string AudioCodec { get; set; }

        [XmlAttribute("videoCodec")]
        public string VideoCodec { get; set; }

        [XmlAttribute("type")]
        public DlnaProfileType Type { get; set; }

        [XmlAttribute("orgPn")]
        public string OrgPn { get; set; }

        [XmlAttribute("mimeType")]
        public string MimeType { get; set; }

        /// <summary>
        /// Gets or sets the container(s) which this profile will be applied to.
        /// </summary>
        public ProfileCondition[] Conditions { get; set; }
    }
}
