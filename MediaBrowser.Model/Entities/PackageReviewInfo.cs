#pragma warning disable CS1591
#pragma warning disable SA1600

using System;

namespace MediaBrowser.Model.Entities
{
    public class PackageReviewInfo
    {
        /// <summary>
        /// The package id (database key) for this review
        /// </summary>
        public int id { get; set; }

        /// <summary>
        /// The rating value
        /// </summary>
        public int rating { get; set; }

        /// <summary>
        /// Whether or not this review recommends this item
        /// </summary>
        public bool recommend { get; set; }

        /// <summary>
        /// A short description of the review
        /// </summary>
        public string title { get; set; }

        /// <summary>
        /// A full review
        /// </summary>
        public string review { get; set; }

        /// <summary>
        /// Time of review
        /// </summary>
        public DateTime timestamp { get; set; }

    }
}
