using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
{
    public class SocketSharpLogger : SocketHttpListener.Logging.ILogger
    {
        private readonly ILogger _logger;

        public SocketSharpLogger(ILogger logger)
        {
            _logger = logger;
        }

        public void Debug(string message, params object[] paramList)
        {
            _logger.Debug(message, paramList);
        }

        public void Error(string message, params object[] paramList)
        {
            _logger.Error(message, paramList);
        }

        public void ErrorException(string message, Exception exception, params object[] paramList)
        {
            _logger.ErrorException(message, exception, paramList);
        }

        public void Fatal(string message, params object[] paramList)
        {
            _logger.Fatal(message, paramList);
        }

        public void FatalException(string message, Exception exception, params object[] paramList)
        {
            _logger.FatalException(message, exception, paramList);
        }

        public void Info(string message, params object[] paramList)
        {
            _logger.Info(message, paramList);
        }

        public void Warn(string message, params object[] paramList)
        {
            _logger.Warn(message, paramList);
        }
    }
}
