using System;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Represents a single video file.
    /// </summary>
    public class AudioBookFileInfo : IComparable<AudioBookFileInfo>
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        public string Container { get; set; }

        /// <summary>
        /// Gets or sets the part number.
        /// </summary>
        /// <value>The part number.</value>
        public int? PartNumber { get; set; }

        /// <summary>
        /// Gets or sets the chapter number.
        /// </summary>
        /// <value>The chapter number.</value>
        public int? ChapterNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is a directory.
        /// </summary>
        /// <value>The type.</value>
        public bool IsDirectory { get; set; }

        /// <inheritdoc />
        public int CompareTo(AudioBookFileInfo other)
        {
            if (ReferenceEquals(this, other))
            {
                return 0;
            }

            if (ReferenceEquals(null, other))
            {
                return 1;
            }

            var chapterNumberComparison = Nullable.Compare(ChapterNumber, other.ChapterNumber);
            if (chapterNumberComparison != 0)
            {
                return chapterNumberComparison;
            }

            var partNumberComparison = Nullable.Compare(PartNumber, other.PartNumber);
            if (partNumberComparison != 0)
            {
                return partNumberComparison;
            }

            return string.Compare(Path, other.Path, StringComparison.Ordinal);
        }
    }
}
