namespace MediaBrowser.Model.Entities
{
    /// <summary>
    /// The status of a series.
    /// </summary>
    public enum SeriesStatus
    {
        /// <summary>
        /// The continuing status. This indicates that a series is currently releasing.
        /// </summary>
        Continuing,

        /// <summary>
        /// The ended status. This indicates that a series has completed and is no longer being released.
        /// </summary>
        Ended,

        /// <summary>
        /// The unreleased status. This indicates that a series has not been released yet.
        /// </summary>
        Unreleased
    }
}
