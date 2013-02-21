using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class ParentalRating
    /// </summary>
    [ProtoContract]
    public class ParentalRating
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(1)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [ProtoMember(2)]
        public int Value { get; set; }
    }
}
