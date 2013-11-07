namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Interface IHasCriticRating
    /// </summary>
    public interface IHasCriticRating
    {
        /// <summary>
        /// Gets or sets the critic rating.
        /// </summary>
        /// <value>The critic rating.</value>
        float? CriticRating { get; set; }

        /// <summary>
        /// Gets or sets the critic rating summary.
        /// </summary>
        /// <value>The critic rating summary.</value>
        string CriticRatingSummary { get; set; }
    }
}
