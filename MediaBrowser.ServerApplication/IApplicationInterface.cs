using System;

namespace MediaBrowser.ServerApplication
{
    /// <summary>
    /// Interface IApplicationInterface
    /// </summary>
    public interface IApplicationInterface
    {
        /// <summary>
        /// Gets a value indicating whether this instance is background service.
        /// </summary>
        /// <value><c>true</c> if this instance is background service; otherwise, <c>false</c>.</value>
        bool IsBackgroundService { get; }

        /// <summary>
        /// Shutdowns the application.
        /// </summary>
        void ShutdownApplication();

        /// <summary>
        /// Restarts the application.
        /// </summary>
        void RestartApplication();

        /// <summary>
        /// Called when [unhandled exception].
        /// </summary>
        /// <param name="ex">The ex.</param>
        void OnUnhandledException(Exception ex);
    }
}
