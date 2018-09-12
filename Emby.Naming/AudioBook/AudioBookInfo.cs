using System.Collections.Generic;
using Emby.Naming.Video;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Represents a complete video, including all parts and subtitles
    /// </summary>
    public class AudioBookInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        public int? Year { get; set; }
        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>The files.</value>
        public List<AudioBookFileInfo> Files { get; set; }
        /// <summary>
        /// Gets or sets the extras.
        /// </summary>
        /// <value>The extras.</value>
        public List<AudioBookFileInfo> Extras { get; set; }
        /// <summary>
        /// Gets or sets the alternate versions.
        /// </summary>
        /// <value>The alternate versions.</value>
        public List<AudioBookFileInfo> AlternateVersions { get; set; }
        
        public AudioBookInfo()
        {
            Files = new List<AudioBookFileInfo>();
            Extras = new List<AudioBookFileInfo>();
            AlternateVersions = new List<AudioBookFileInfo>();
        }
    }
}
