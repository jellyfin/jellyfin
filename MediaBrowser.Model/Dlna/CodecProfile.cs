#nullable disable
#pragma warning disable CS1591

using System;
using System.Xml.Serialization;
using Jellyfin.Extensions;

namespace MediaBrowser.Model.Dlna
{
    public class CodecProfile
    {
        public CodecProfile()
        {
            Conditions = Array.Empty<ProfileCondition>();
            ApplyConditions = Array.Empty<ProfileCondition>();
        }

        [XmlAttribute("type")]
        public CodecType Type { get; set; }

        public ProfileCondition[] Conditions { get; set; }

        public ProfileCondition[] ApplyConditions { get; set; }

        [XmlAttribute("codec")]
        public string Codec { get; set; }

        [XmlAttribute("container")]
        public string Container { get; set; }

        public string[] GetCodecs()
        {
            return ContainerProfile.SplitValue(Codec);
        }

        private bool ContainsContainer(string container)
        {
            return ContainerProfile.ContainsContainer(Container, container);
        }

        public bool ContainsAnyCodec(string codec, string container)
        {
            return ContainsAnyCodec(ContainerProfile.SplitValue(codec), container);
        }

        public bool ContainsAnyCodec(string[] codec, string container)
        {
            if (!ContainsContainer(container))
            {
                return false;
            }

            var codecs = GetCodecs();
            if (codecs.Length == 0)
            {
                return true;
            }

            foreach (var val in codec)
            {
                if (codecs.Contains(val, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
