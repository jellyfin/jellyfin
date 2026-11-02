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
        /// Compares two <see cref="AudioBookNameParserResult"/> instances for equality.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if the instances are equal; otherwise, false.</returns>
        public static bool operator ==(AudioBookNameParserResult left, AudioBookNameParserResult right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="AudioBookNameParserResult"/> instances for inequality.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if the instances are not equal; otherwise, false.</returns>
        public static bool operator !=(AudioBookNameParserResult left, AudioBookNameParserResult right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is AudioBookNameParserResult other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(AudioBookNameParserResult other)
            => string.Equals(Name, other.Name, StringComparison.Ordinal)
                && Year.Equals(other.Year);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Name, Year);
    }
}
