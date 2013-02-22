using MediaBrowser.Model.Logging;
using System;
using System.Text;

namespace MediaBrowser.Logging.Nlog
{
    /// <summary>
    /// Class NLogger
    /// </summary>
    public class NLogger : ILogger
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly NLog.Logger _logger;

        /// <summary>
        /// The _lock object
        /// </summary>
        private static readonly object LockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="NLogger" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public NLogger(string name)
        {
            lock (LockObject)
            {
                _logger = NLog.LogManager.GetLogger(name);
            }
        }

        /// <summary>
        /// Infoes the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public void Info(string message, params object[] paramList)
        {
            _logger.Info(message, paramList);
        }

        /// <summary>
        /// Errors the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public void Error(string message, params object[] paramList)
        {
            _logger.Error(message, paramList);
        }

        /// <summary>
        /// Warns the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public void Warn(string message, params object[] paramList)
        {
            _logger.Warn(message, paramList);
        }

        /// <summary>
        /// Debugs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public void Debug(string message, params object[] paramList)
        {
            _logger.Debug(message, paramList);
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="paramList">The param list.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void ErrorException(string message, Exception exception, params object[] paramList)
        {
            LogException(LogSeverity.Error, message, exception, paramList);
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="paramList">The param list.</param>
        private void LogException(LogSeverity level, string message, Exception exception, params object[] paramList)
        {
            message = FormatMessage(message, paramList).Replace(Environment.NewLine, ". ");

            var messageText = LogHelper.GetLogMessage(exception);

            LogMultiline(message, level, messageText);
        }

        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        /// <returns>System.String.</returns>
        private static string FormatMessage(string message, params object[] paramList)
        {
            if (paramList != null)
            {
                for (var i = 0; i < paramList.Length; i++)
                {
                    message = message.Replace("{" + i + "}", paramList[i].ToString());
                }
            }

            return message;
        }

        /// <summary>
        /// Logs the multiline.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="severity">The severity.</param>
        /// <param name="additionalContent">Content of the additional.</param>
        public void LogMultiline(string message, LogSeverity severity, StringBuilder additionalContent)
        {
            additionalContent.Insert(0, message + Environment.NewLine);

            const char tabChar = '\t';

            var text = additionalContent.ToString()
                                        .Replace(Environment.NewLine, Environment.NewLine + tabChar)
                                        .TrimEnd(tabChar);

            if (text.EndsWith(Environment.NewLine))
            {
                text = text.Substring(0, text.LastIndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase));
            }

            _logger.Log(GetLogLevel(severity), text);
        }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <returns>NLog.LogLevel.</returns>
        private NLog.LogLevel GetLogLevel(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Debug:
                    return NLog.LogLevel.Debug;
                case LogSeverity.Error:
                    return NLog.LogLevel.Error;
                case LogSeverity.Warn:
                    return NLog.LogLevel.Warn;
                case LogSeverity.Fatal:
                    return NLog.LogLevel.Fatal;
                case LogSeverity.Info:
                    return NLog.LogLevel.Info;
                default:
                    throw new ArgumentException("Unknown LogSeverity: " + severity.ToString());
            }
        }

        /// <summary>
        /// Logs the specified severity.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public void Log(LogSeverity severity, string message, params object[] paramList)
        {
            _logger.Log(GetLogLevel(severity), message, paramList);
        }

        /// <summary>
        /// Fatals the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="paramList">The param list.</param>
        public void Fatal(string message, params object[] paramList)
        {
            _logger.Fatal(message, paramList);
        }

        /// <summary>
        /// Fatals the exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="paramList">The param list.</param>
        public void FatalException(string message, Exception exception, params object[] paramList)
        {
            LogException(LogSeverity.Fatal, message, exception, paramList);
        }
    }
}
