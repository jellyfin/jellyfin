using System.Diagnostics;
using System.Runtime.Serialization;

namespace MediaBrowser.Model.Dto
{
    /// <summary>
    /// Class ChapterInfo
    /// </summary>
    [DebuggerDisplay("Name = {Name}")]
    public class ChapterInfoDto
    {
        /// <summary>
        /// Gets or sets the start position ticks.
        /// </summary>
        /// <value>The start position ticks.</value>
        public long StartPositionTicks { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the image tag.
        /// </summary>
        /// <value>The image tag.</value>
        public string ImageTag { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has image.
        /// </summary>
        /// <value><c>true</c> if this instance has image; otherwise, <c>false</c>.</value>
        [IgnoreDataMember]
        public bool HasImage
        {
            get { return ImageTag != null; }
        }
    }
}
