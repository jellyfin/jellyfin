using System;

namespace Emby.Naming.Video
{
    /// <summary>
    /// Holder structure for name and year.
    /// </summary>
    public readonly struct CleanDateTimeResult : IEquatable<CleanDateTimeResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CleanDateTimeResult"/> struct.
        /// </summary>
        /// <param name="name">Name of video.</param>
        /// <param name="year">Year of release.</param>
        public CleanDateTimeResult(string name, int? year = null)
        {
            Name = name;
            Year = year;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the year.
        /// </summary>
        /// <value>The year.</value>
        public int? Year { get; }

        /// <summary>
        /// Compares two <see cref="CleanDateTimeResult"/> instances for equality.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if the instances are equal; otherwise, false.</returns>
        public static bool operator ==(CleanDateTimeResult left, CleanDateTimeResult right)
            => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="CleanDateTimeResult"/> instances for inequality.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>True if the instances are not equal; otherwise, false.</returns>
        public static bool operator !=(CleanDateTimeResult left, CleanDateTimeResult right)
            => !left.Equals(right);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
            => obj is CleanDateTimeResult other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(CleanDateTimeResult other)
            => string.Equals(Name, other.Name, StringComparison.Ordinal)
                && Year.Equals(other.Year);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(Name, Year);
    }
}
