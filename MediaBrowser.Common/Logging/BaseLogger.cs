using System;
using System.Text;
using System.Threading;

namespace MediaBrowser.Common.Logging
{
    public abstract class BaseLogger : IDisposable
    {
        public LogSeverity LogSeverity { get; set; }

        public void LogInfo(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Info, paramList);
        }

        public void LogDebugInfo(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Debug, paramList);
        }

        public void LogError(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Error, paramList);
        }

        public void LogException(string message, Exception exception, params object[] paramList)
        {
            var builder = new StringBuilder();

            if (exception != null)
            {
                builder.AppendFormat("Exception.  Type={0} Msg={1} StackTrace={3}{2}",
                    exception.GetType().FullName,
                    exception.Message,
                    exception.StackTrace,
                    Environment.NewLine);
            }

            message = FormatMessage(message, paramList);

            LogError(string.Format("{0} ( {1} )", message, builder));
        }

        public void LogWarning(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Warning, paramList);
        }

        private string FormatMessage(string message, params object[] paramList)
        {
            if (paramList != null)
            {
                for (int i = 0; i < paramList.Length; i++)
                {
                    message = message.Replace("{" + i + "}", paramList[i].ToString());
                }
            }

            return message;
        }

        private void LogEntry(string message, LogSeverity severity, params object[] paramList)
        {
            if (severity < LogSeverity) return;

            message = FormatMessage(message, paramList);

            Thread currentThread = Thread.CurrentThread;

            var row = new LogRow
            {
                Severity = severity,
                Message = message,
                ThreadId = currentThread.ManagedThreadId,
                ThreadName = currentThread.Name,
                Time = DateTime.Now
            };

            LogEntry(row);
        }

        protected virtual void Flush()
        {
        }

        public virtual void Dispose()
        {
        }

        protected abstract void LogEntry(LogRow row);
    }
}
