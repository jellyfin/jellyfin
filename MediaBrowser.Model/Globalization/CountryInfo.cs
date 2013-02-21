using ProtoBuf;

namespace MediaBrowser.Model.Globalization
{
    /// <summary>
    /// Class CountryInfo
    /// </summary>
    [ProtoContract]
    public class CountryInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        /// <value>The display name.</value>
        [ProtoMember(2)]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the name of the two letter ISO region.
        /// </summary>
        /// <value>The name of the two letter ISO region.</value>
        [ProtoMember(3)]
        public string TwoLetterISORegionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the three letter ISO region.
        /// </summary>
        /// <value>The name of the three letter ISO region.</value>
        [ProtoMember(4)]
        public string ThreeLetterISORegionName { get; set; }
    }
}
