using MediaBrowser.Model.Logging;
using ServiceStack.Logging;
using System;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class ServerLogFactory
    /// </summary>
    public class ServerLogFactory : ILogFactory
    {
        /// <summary>
        /// The _log manager
        /// </summary>
        private readonly ILogManager _logManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerLogFactory"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        public ServerLogFactory(ILogManager logManager)
        {
            _logManager = logManager;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>ILog.</returns>
        public ILog GetLogger(string typeName)
        {
            return new ServerLogger(_logManager.GetLogger(typeName));
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>ILog.</returns>
        public ILog GetLogger(Type type)
        {
            return GetLogger(type.Name);
        }
    }

    /// <summary>
    /// Class ServerLogger
    /// </summary>
    public class ServerLogger : ILog
    {
        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ServerLogger(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Logs a Debug message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Debug(object message, Exception exception)
        {
            _logger.ErrorException(GetMesssage(message), exception);
        }

        /// <summary>
        /// Logs a Debug message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Debug(object message)
        {
            // Way too verbose. Can always make this configurable if needed again.
            //_logger.Debug(GetMesssage(message));
        }

        /// <summary>
        /// Logs a Debug format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void DebugFormat(string format, params object[] args)
        {
            // Way too verbose. Can always make this configurable if needed again.
            //_logger.Debug(format, args);
        }

        /// <summary>
        /// Logs a Error message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Error(object message, Exception exception)
        {
            _logger.ErrorException(GetMesssage(message), exception);
        }

        /// <summary>
        /// Logs a Error message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Error(object message)
        {
            _logger.Error(GetMesssage(message));
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void ErrorFormat(string format, params object[] args)
        {
            _logger.Error(format, args);
        }

        /// <summary>
        /// Logs a Fatal message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Fatal(object message, Exception exception)
        {
            _logger.FatalException(GetMesssage(message), exception);
        }

        /// <summary>
        /// Logs a Fatal message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Fatal(object message)
        {
            _logger.Fatal(GetMesssage(message));
        }

        /// <summary>
        /// Logs a Error format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void FatalFormat(string format, params object[] args)
        {
            _logger.Fatal(format, args);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Info(object message, Exception exception)
        {
            _logger.ErrorException(GetMesssage(message), exception);
        }

        /// <summary>
        /// Logs an Info message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Info(object message)
        {
            _logger.Info(GetMesssage(message));
        }

        /// <summary>
        /// Logs an Info format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void InfoFormat(string format, params object[] args)
        {
            _logger.Info(format, args);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is debug enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is debug enabled; otherwise, <c>false</c>.</value>
        public bool IsDebugEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// Logs a Warning message and exception.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public void Warn(object message, Exception exception)
        {
            _logger.ErrorException(GetMesssage(message), exception);
        }

        /// <summary>
        /// Logs a Warning message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Warn(object message)
        {
            // Hide StringMapTypeDeserializer messages
            // _logger.Warn(GetMesssage(message));
        }

        /// <summary>
        /// Logs a Warning format message.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The args.</param>
        public void WarnFormat(string format, params object[] args)
        {
            // Hide StringMapTypeDeserializer messages
            // _logger.Warn(format, args);
        }

        /// <summary>
        /// Gets the messsage.
        /// </summary>
        /// <param name="o">The o.</param>
        /// <returns>System.String.</returns>
        private string GetMesssage(object o)
        {
            return o == null ? string.Empty : o.ToString();
        }
    }
}
