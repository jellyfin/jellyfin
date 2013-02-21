using ProtoBuf;

namespace MediaBrowser.Model.Globalization
{
    /// <summary>
    /// Class CultureDto
    /// </summary>
    [ProtoContract]
    public class CultureDto
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
        /// Gets or sets the name of the two letter ISO language.
        /// </summary>
        /// <value>The name of the two letter ISO language.</value>
        [ProtoMember(3)]
        public string TwoLetterISOLanguageName { get; set; }

        /// <summary>
        /// Gets or sets the name of the three letter ISO language.
        /// </summary>
        /// <value>The name of the three letter ISO language.</value>
        [ProtoMember(4)]
        public string ThreeLetterISOLanguageName { get; set; }
    }
}
