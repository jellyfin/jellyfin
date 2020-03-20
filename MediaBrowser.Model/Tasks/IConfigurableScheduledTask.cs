#pragma warning disable CS1591

namespace MediaBrowser.Model.Tasks
{
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

        bool IsLogged { get; }
    }
}
