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

        /// <summary>
        /// Equality operator for <see cref="AudioBookNameParserResult"/>.
        /// </summary>
        /// <param name="left">Source comparison object.</param>
        /// <param name="right">Target comparision object.</param>
        /// <returns>If both objects are the equal.</returns>
        public static bool operator ==(AudioBookNameParserResult left, AudioBookNameParserResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for <see cref="AudioBookNameParserResult"/>.
        /// </summary>
        /// <param name="left">Source comparison object.</param>
        /// <param name="right">Target comparision object.</param>
        /// <returns>If both objects are the equal.</returns>
        public static bool operator !=(AudioBookNameParserResult left, AudioBookNameParserResult right)
        {
            return !(left == right);
        }

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
