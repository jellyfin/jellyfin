using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace MediaBrowser.Common.Logging
{
    public abstract class BaseLogger
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
            StringBuilder builder = new StringBuilder();

            if (exception != null)
            {
                var trace = new StackTrace(exception, true);
                builder.AppendFormat("Exception.  Type={0} Msg={1} Src={2} Method={5} Line={6} Col={7}{4}StackTrace={4}{3}",
                    exception.GetType().FullName,
                    exception.Message,
                    exception.Source,
                    exception.StackTrace,
                    Environment.NewLine,
                    trace.GetFrame(0).GetMethod().Name,
                    trace.GetFrame(0).GetFileLineNumber(),
                    trace.GetFrame(0).GetFileColumnNumber());
            }

            StackFrame frame = new StackFrame(1);

            message = string.Format(message, paramList);

            LogError(string.Format("{0} ( {1} )", message, builder));
        }

        public void LogWarning(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Warning, paramList);
        }

        private void LogEntry(string message, LogSeverity severity, params object[] paramList)
        {
            if (severity < LogSeverity) return;
            
            message = string.Format(message, paramList);

            Thread currentThread = Thread.CurrentThread;

            LogRow row = new LogRow()
            {
                Severity = severity,
                Message = message,
                Category = string.Empty,
                ThreadId = currentThread.ManagedThreadId,
                ThreadName = currentThread.Name,
                Time = DateTime.Now
            };

            LogEntry(row);
        }

        protected abstract void LogEntry(LogRow row);
    }
}
