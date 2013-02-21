using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.Common.Logging
{
    /// <summary>
    /// Class Logger
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets or sets the logger instance.
        /// </summary>
        /// <value>The logger instance.</value>
        internal static ILogger LoggerInstance { get; set; }

        /// <summary>
        /// Logs the info.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public static void LogInfo(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Info, null, paramList);
        }

        /// <summary>
        /// Logs the debug info.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public static void LogDebugInfo(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Debug, null, paramList);
        }
        
        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public static void LogError(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Error, null, paramList);
        }
        
        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="paramList">The param list.</param>
        public static void LogException(string message, Exception ex, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Error, ex, paramList);
        }

        /// <summary>
        /// Fatals the exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="paramList">The param list.</param>
        public static void FatalException(string message, Exception ex, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Fatal, ex, paramList);
        }
        
        /// <summary>
        /// Logs the warning.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public static void LogWarning(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Warn, null, paramList);
        }

        /// <summary>
        /// Logs the entry.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="level">The level.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="paramList">The param list.</param>
        private static void LogEntry(string message, LogSeverity level, Exception exception, params object[] paramList)
        {
            if (LoggerInstance == null)
            {
                return;
            }

            if (exception == null)
            {
                LoggerInstance.Log(level, message, paramList);
            }
            else
            {
                if (level == LogSeverity.Fatal)
                {
                    LoggerInstance.FatalException(message, exception, paramList);
                }
                else
                {
                    LoggerInstance.ErrorException(message, exception, paramList);
                }
            }
        }
    }
}
