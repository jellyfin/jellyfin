#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Querying
{
    public class EpisodeQuery
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the season identifier.
        /// </summary>
        /// <value>The season identifier.</value>
        public string SeasonId { get; set; }

        /// <summary>
        /// Gets or sets the series identifier.
        /// </summary>
        /// <value>The series identifier.</value>
        public string SeriesId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is missing.
        /// </summary>
        /// <value><c>null</c> if [is missing] contains no value, <c>true</c> if [is missing]; otherwise, <c>false</c>.</value>
        public bool? IsMissing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is virtual unaired.
        /// </summary>
        /// <value><c>null</c> if [is virtual unaired] contains no value, <c>true</c> if [is virtual unaired]; otherwise, <c>false</c>.</value>
        public bool? IsVirtualUnaired { get; set; }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        /// <value>The season number.</value>
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the fields.
        /// </summary>
        /// <value>The fields.</value>
        public ItemFields[] Fields { get; set; }

        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets the start item identifier.
        /// </summary>
        /// <value>The start item identifier.</value>
        public string StartItemId { get; set; }

        public EpisodeQuery()
        {
            Fields = Array.Empty<ItemFields>();
        }
    }
}
