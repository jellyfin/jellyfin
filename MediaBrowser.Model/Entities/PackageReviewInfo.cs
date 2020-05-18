#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Entities
{
    public class PackageReviewInfo
    {
        /// <summary>
        /// Gets or sets the package id (database key) for this review.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the rating value.
        /// </summary>
        public int Rating { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this review recommends this item.
        /// </summary>
        public bool Recommend { get; set; }

        /// <summary>
        /// Gets or sets a short description of the review.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the full review.
        /// </summary>
        public string Review { get; set; }

        /// <summary>
        /// Gets or sets the time of review.
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}
