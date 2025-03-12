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
        /// Equallity operator for <see cref="CleanDateTimeResult"/>.
        /// </summary>
        /// <param name="left">Source comparison object.</param>
        /// <param name="right">Target comparision object.</param>
        /// <returns>If both objects are the equal.</returns>
        public static bool operator ==(CleanDateTimeResult left, CleanDateTimeResult right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operators for <see cref="CleanDateTimeResult"/>.
        /// </summary>
        /// <param name="left">Source comparison object.</param>
        /// <param name="right">Target comparision object.</param>
        /// <returns>If both objects are the equal.</returns>
        public static bool operator !=(CleanDateTimeResult left, CleanDateTimeResult right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not CleanDateTimeResult other)
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
        public bool Equals(CleanDateTimeResult other)
        {
            return string.Equals(Name, other.Name, StringComparison.Ordinal) && int.Equals(Year, other.Year);
        }
    }
}
