using System.Collections.Generic;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Represents a complete video, including all parts and subtitles
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the year.
        /// </summary>
        /// <value>The year.</value>
        public int? Year { get; set; }
        /// <summary>
        /// Gets or sets the files.
        /// </summary>
        /// <value>The files.</value>
        public List<VideoFileInfo> Files { get; set; }
        /// <summary>
        /// Gets or sets the extras.
        /// </summary>
        /// <value>The extras.</value>
        public List<VideoFileInfo> Extras { get; set; }
        /// <summary>
        /// Gets or sets the alternate versions.
        /// </summary>
        /// <value>The alternate versions.</value>
        public List<VideoFileInfo> AlternateVersions { get; set; }
        
        public VideoInfo()
        {
            Files = new List<VideoFileInfo>();
            Extras = new List<VideoFileInfo>();
            AlternateVersions = new List<VideoFileInfo>();
        }
    }
}
