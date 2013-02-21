using ProtoBuf;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class ChapterInfo
    /// </summary>
    [ProtoContract]
    public class ChapterInfo
    {
        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        [ProtoMember(1)]
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [ProtoMember(2)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the image path.
        /// </summary>
        /// <value>The image path.</value>
        [ProtoMember(3)]
        public string ImagePath { get; set; }
    }
}
