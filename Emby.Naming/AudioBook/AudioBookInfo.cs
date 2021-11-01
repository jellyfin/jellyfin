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
        public AudioBookInfo(string name, int? year, IReadOnlyList<AudioBookFileInfo> files, IReadOnlyList<AudioBookFileInfo> extras, IReadOnlyList<AudioBookFileInfo> alternateVersions)
        {
            Name = name;
            Year = year;
            Files = files;
            Extras = extras;
            AlternateVersions = alternateVersions;
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
        public IReadOnlyList<AudioBookFileInfo> Files { get; set; }

        /// <summary>
        /// Gets or sets the extras.
        /// </summary>
        /// <value>The extras.</value>
        public IReadOnlyList<AudioBookFileInfo> Extras { get; set; }

        /// <summary>
        /// Gets or sets the alternate versions.
        /// </summary>
        /// <value>The alternate versions.</value>
        public IReadOnlyList<AudioBookFileInfo> AlternateVersions { get; set; }
    }
}
