using System.Collections.Generic;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Represents a complete video, including all parts and subtitles.
    /// </summary>
    public class AudioBookInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookInfo" /> class.
        /// </summary>
        /// <param name="name">Name of audiobook.</param>
        /// <param name="year">Year of audiobook release.</param>
        /// <param name="files">List of files composing the actual audiobook.</param>
        /// <param name="extras">List of extra files.</param>
        /// <param name="alternateVersions">Alternative version of files.</param>
        public AudioBookInfo(string name, int? year, List<AudioBookFileInfo>? files, List<AudioBookFileInfo>? extras, List<AudioBookFileInfo>? alternateVersions)
        {
            Name = name;
            Year = year;
            Files = files ?? new List<AudioBookFileInfo>();
            Extras = extras ?? new List<AudioBookFileInfo>();
            AlternateVersions = alternateVersions ?? new List<AudioBookFileInfo>();
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the year.
        /// </summary>
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
    }
}
