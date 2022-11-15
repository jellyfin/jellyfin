namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Enum TaskCompletionStatus.
    /// </summary>
    public enum TaskCompletionStatus
    {
        /// <summary>
        /// The completed.
        /// </summary>
        Completed,

        /// <summary>
        /// The failed.
        /// </summary>
        Failed,

        /// <summary>
        /// Manually cancelled by the user.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Aborted due to a system failure or shutdown.
        /// </summary>
        Aborted
    }
}
