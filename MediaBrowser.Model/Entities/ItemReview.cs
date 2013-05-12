using System;

namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// Class ItemReview
    /// </summary>
    public class ItemReview
    {
        /// <summary>
        /// Gets or sets the name of the reviewer.
        /// </summary>
        /// <value>The name of the reviewer.</value>
        public string ReviewerName { get; set; }

        /// <summary>
        /// Gets or sets the publisher.
        /// </summary>
        /// <value>The publisher.</value>
        public string Publisher { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>The date.</value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the score.
        /// </summary>
        /// <value>The score.</value>
        public float? Score { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ItemReview"/> is likes.
        /// </summary>
        /// <value><c>null</c> if [likes] contains no value, <c>true</c> if [likes]; otherwise, <c>false</c>.</value>
        public bool? Likes { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        /// <value>The caption.</value>
        public string Caption { get; set; }
    }
}
