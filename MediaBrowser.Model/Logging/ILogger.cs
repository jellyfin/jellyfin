using System;
using System.Text;

namespace MediaBrowser.Model.Logging
{
    /// <summary>
    /// Interface ILogger
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Infoes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        void Info(string message, params object[] paramList);

        /// <summary>
        /// Errors the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        void Error(string message, params object[] paramList);

        /// <summary>
        /// Warns the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        void Warn(string message, params object[] paramList);

        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        void Debug(string message, params object[] paramList);

        /// <summary>
        /// Fatals the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        void Fatal(string message, params object[] paramList);

        /// <summary>
        /// Fatals the exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="paramList">The param list.</param>
        void FatalException(string message, Exception exception, params object[] paramList);
        
        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="paramList">The param list.</param>
        void ErrorException(string message, Exception exception, params object[] paramList);

        /// <summary>
        /// Logs the multiline.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="severity">The severity.</param>
        /// <param name="additionalContent">Content of the additional.</param>
        void LogMultiline(string message, LogSeverity severity, StringBuilder additionalContent);

        /// <summary>
        /// Logs the specified severity.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The parameter list.</param>
        void Log(LogSeverity severity, string message, params object[] paramList);
    }
}
