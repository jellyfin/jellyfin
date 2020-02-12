using System;
using System.Collections.Generic;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Represents a complete video, including all parts and subtitles.
    /// </summary>
    public class VideoInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoInfo" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public VideoInfo(string name)
        {
            Name = name;

            Files = Array.Empty<VideoFileInfo>();
            Extras = Array.Empty<VideoFileInfo>();
            AlternateVersions = Array.Empty<VideoFileInfo>();
        }

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
        public IReadOnlyList<VideoFileInfo> Files { get; set; }

        /// <summary>
        /// Gets or sets the extras.
        /// </summary>
        /// <value>The extras.</value>
        public IReadOnlyList<VideoFileInfo> Extras { get; set; }

        /// <summary>
        /// Gets or sets the alternate versions.
        /// </summary>
        /// <value>The alternate versions.</value>
        public IReadOnlyList<VideoFileInfo> AlternateVersions { get; set; }
    }
}
