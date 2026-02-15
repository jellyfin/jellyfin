namespace MediaBrowser.Model.Tasks
{
    /// <summary>
    /// Interface for configurable scheduled tasks.
    /// </summary>
    public interface IConfigurableScheduledTask
    {
        /// <summary>
        /// Gets a value indicating whether this instance is hidden.
        /// </summary>
        /// <value><c>true</c> if this instance is hidden; otherwise, <c>false</c>.</value>
        bool IsHidden { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is enabled; otherwise, <c>false</c>.</value>
        bool IsEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is logged.
        /// </summary>
        /// <value><c>true</c> if this instance is logged; otherwise, <c>false</c>.</value>
        bool IsLogged { get; }
    }
}
