#pragma warning disable CS1591
#nullable enable

namespace Emby.Naming.Video
{
    public readonly struct CleanDateTimeResult
    {
        public CleanDateTimeResult(string name, int? year)
        {
            Name = name;
            Year = year;
        }

        public CleanDateTimeResult(string name)
        {
            Name = name;
            Year = null;
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
    }
}
