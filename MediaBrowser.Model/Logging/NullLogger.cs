using System;
using System.Text;

namespace MediaBrowser.Model.Logging
{
    public class NullLogger : ILogger
    {
        public void Info(string message, params object[] paramList)
        {
        }

        public void Error(string message, params object[] paramList)
        {
        }

        public void Warn(string message, params object[] paramList)
        {
        }

        public void Debug(string message, params object[] paramList)
        {
        }

        public void Fatal(string message, params object[] paramList)
        {
        }

        public void FatalException(string message, Exception exception, params object[] paramList)
        {
        }

        public void Log(LogSeverity severity, string message, params object[] paramList)
        {
        }

        public void ErrorException(string message, Exception exception, params object[] paramList)
        {
        }

        public void LogMultiline(string message, LogSeverity severity, StringBuilder additionalContent)
        {
        }
    }
}
