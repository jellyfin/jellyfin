#pragma warning disable CS1591

namespace Emby.Naming.TV
{
    public class SeasonPathParserResult
    {
        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        /// <value>The season number.</value>
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SeasonPathParserResult" /> is success.
        /// </summary>
        /// <value><c>true</c> if success; otherwise, <c>false</c>.</value>
        public bool Success { get; set; }

        public bool IsSeasonFolder { get; set; }
    }
}
