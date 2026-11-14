using System;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Represents a single video file.
    /// </summary>
    public class AudioBookFileInfo : IComparable<AudioBookFileInfo>, IEquatable<AudioBookFileInfo>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AudioBookFileInfo"/> class.
        /// </summary>
        /// <param name="path">Path to audiobook file.</param>
        /// <param name="container">File type.</param>
        /// <param name="partNumber">Number of part this file represents.</param>
        /// <param name="chapterNumber">Number of chapter this file represents.</param>
        public AudioBookFileInfo(string path, string container, int? partNumber = default, int? chapterNumber = default)
        {
            Path = path;
            Container = container;
            PartNumber = partNumber;
            ChapterNumber = chapterNumber;
        }

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
        /// Compares two <see cref="AudioBookFileInfo"/> instances for equality.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns><c>true</c> if the operands are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(AudioBookFileInfo? left, AudioBookFileInfo? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        /// <summary>
        /// Compares two <see cref="AudioBookFileInfo"/> instances for inequality.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns><c>true</c> if the operands are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(AudioBookFileInfo? left, AudioBookFileInfo? right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the left-hand operand is less than the right-hand operand.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns><c>true</c> if the left-hand operand is less than the right-hand operand; otherwise, <c>false</c>.</returns>
        public static bool operator <(AudioBookFileInfo? left, AudioBookFileInfo? right)
        {
            return left is null ? right is not null : left.CompareTo(right) < 0;
        }

        /// <summary>
        /// Determines whether the left-hand operand is less than or equal to the right-hand operand.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns><c>true</c> if the left-hand operand is less than or equal to the right-hand operand; otherwise, <c>false</c>.</returns>
        public static bool operator <=(AudioBookFileInfo? left, AudioBookFileInfo? right)
        {
            return left is null || left.CompareTo(right) <= 0;
        }

        /// <summary>
        /// Determines whether the left-hand operand is greater than the right-hand operand.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns><c>true</c> if the left-hand operand is greater than the right-hand operand; otherwise, <c>false</c>.</returns>
        public static bool operator >(AudioBookFileInfo? left, AudioBookFileInfo? right)
        {
            return left is not null && left.CompareTo(right) > 0;
        }

        /// <summary>
        /// Determines whether the left-hand operand is greater than or equal to the right-hand operand.
        /// </summary>
        /// <param name="left">The left-hand operand.</param>
        /// <param name="right">The right-hand operand.</param>
        /// <returns><c>true</c> if the left-hand operand is greater than or equal to the right-hand operand; otherwise, <c>false</c>.</returns>
        public static bool operator >=(AudioBookFileInfo? left, AudioBookFileInfo? right)
        {
            return left is null ? right is null : left.CompareTo(right) >= 0;
        }

        /// <inheritdoc />
        public int CompareTo(AudioBookFileInfo? other)
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

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as AudioBookFileInfo);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> parameter; otherwise, <c>false</c>.</returns>
        public bool Equals(AudioBookFileInfo? other)
        {
            if (other is null)
            {
                return false;
            }

            return ChapterNumber == other.ChapterNumber
                && PartNumber == other.PartNumber
                && string.Equals(Container, other.Container, StringComparison.Ordinal)
                && string.Equals(Path, other.Path, StringComparison.Ordinal);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(Path, Container, PartNumber, ChapterNumber);
        }
    }
}
