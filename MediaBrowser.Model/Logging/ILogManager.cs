using System;

namespace MediaBrowser.Model.Logging
{
    /// <summary>
    /// Interface ILogManager
    /// </summary>
    public interface ILogManager
    {
        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>The log level.</value>
        LogSeverity LogSeverity { get; set; }

        /// <summary>
        /// Gets or sets the exception message prefix.
        /// </summary>
        /// <value>The exception message prefix.</value>
        string ExceptionMessagePrefix { get; set; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ILogger.</returns>
        ILogger GetLogger(string name);

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        void ReloadLogger(LogSeverity severity);

        /// <summary>
        /// Gets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        string LogFilePath { get; }

        /// <summary>
        /// Occurs when [logger loaded].
        /// </summary>
        event EventHandler LoggerLoaded;

        /// <summary>
        /// Flushes this instance.
        /// </summary>
        void Flush();

        /// <summary>
        /// Adds the console output.
        /// </summary>
        void AddConsoleOutput();

        /// <summary>
        /// Removes the console output.
        /// </summary>
        void RemoveConsoleOutput();
    }
}
