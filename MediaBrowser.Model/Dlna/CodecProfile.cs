using System;
using System.Linq;
using System.Xml.Serialization;

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="CodecProfile" />.
    /// </summary>
    public class CodecProfile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodecProfile"/> class.
        /// </summary>
        public CodecProfile()
        {
            Conditions = Array.Empty<ProfileCondition>();
            ApplyConditions = Array.Empty<ProfileCondition>();
            Codec = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodecProfile"/> class.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <param name="type">The <see cref="CodecType"/>.</param>
        /// <param name="conditions">An array of <see cref="ProfileCondition"/>.</param>
        public CodecProfile(string? codec, CodecType type, ProfileCondition[] conditions)
        {
            Codec = codec;
            Type = type;
            Conditions = conditions;
            ApplyConditions = Array.Empty<ProfileCondition>();
        }

        /// <summary>
        /// Gets or sets the Type.
        /// </summary>
        [XmlAttribute("type")]
        public CodecType Type { get; set; }

        /// <summary>
        /// Gets or sets the profile conditions..
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public ProfileCondition[] Conditions { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the conditions that should be applied..
        /// </summary>
#pragma warning disable CA1819 // Properties should not return arrays
        public ProfileCondition[] ApplyConditions { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Gets or sets the Codec.
        /// </summary>
        [XmlAttribute("codec")]
        public string? Codec { get; set; }

        /// <summary>
        /// Gets or sets the Container.
        /// </summary>
        [XmlAttribute("container")]
        public string? Container { get; set; }

        /// <summary>
        /// Checks to see if the container contains any of the codecs.
        /// </summary>
        /// <param name="codec">The codec.</param>
        /// <param name="container">The container.</param>
        /// <returns>True if <paramref name="container"/> is supported and it contains <paramref name="codec"/>.</returns>
        public bool ContainsAnyCodec(string? codec, string? container)
        {
            if (codec == null || container == null)
            {
                return false;
            }

            var destCodec = ContainerProfile.SplitValue(codec);

            if (!ContainerProfile.ContainsContainer(Container, container))
            {
                return false;
            }

            var codecs = ContainerProfile.SplitValue(Codec);
            if (codecs.Length == 0)
            {
                return true;
            }

            foreach (var val in destCodec)
            {
                if (codecs.Contains(val, StringComparer.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
