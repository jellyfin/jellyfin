using System;

namespace Emby.Naming.AudioBook
{
    /// <summary>
    /// Data object used to pass result of name and year parsing.
    /// </summary>
    public struct AudioBookNameParserResult : IEquatable<AudioBookNameParserResult>
    {
        /// <summary>
        /// Gets or sets name of audiobook.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets optional year of release.
        /// </summary>
        public int? Year { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public static bool operator ==(AudioBookNameParserResult left, AudioBookNameParserResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AudioBookNameParserResult left, AudioBookNameParserResult right)
        {
            return !(left == right);
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not AudioBookNameParserResult other)
            {
                return false;
            }

            return Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(Name, Year);
        }

        /// <inheritdoc/>
        public bool Equals(AudioBookNameParserResult other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal) && int.Equals(Year, other.Year);
        }
    }
}
