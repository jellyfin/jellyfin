using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

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
        public VideoInfo(string? name)
        {
            Name = name;

            Files = [];
            AlternateVersions = [];
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

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
        /// Gets or sets the alternate versions. Each alternate may itself span multiple files.
        /// </summary>
        /// <value>The alternate versions.</value>
        public IReadOnlyList<VideoInfo> AlternateVersions { get; set; }

        /// <summary>
        /// Gets or sets the extra type.
        /// </summary>
        public ExtraType? ExtraType { get; set; }
    }
}
