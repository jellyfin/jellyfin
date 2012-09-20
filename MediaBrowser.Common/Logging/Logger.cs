using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using MediaBrowser.Common.Kernel;

namespace MediaBrowser.Common.Logging
{
    public static class Logger
    {
        internal static IKernel Kernel { get; set; }

        public static void LogInfo(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Info, paramList);
        }

        public static void LogDebugInfo(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Debug, paramList);
        }

        public static void LogError(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Error, paramList);
        }

        public static void LogException(Exception ex, params object[] paramList)
        {
            LogException(string.Empty, ex, paramList);
        }

        public static void LogException(string message, Exception ex, params object[] paramList)
        {
            var builder = new StringBuilder();

            if (ex != null)
            {
                builder.AppendFormat("Exception.  Type={0} Msg={1} StackTrace={3}{2}",
                    ex.GetType().FullName,
                    ex.Message,
                    ex.StackTrace,
                    Environment.NewLine);
            }

            message = FormatMessage(message, paramList);

            LogError(string.Format("{0} ( {1} )", message, builder));
        }

        public static void LogWarning(string message, params object[] paramList)
        {
            LogEntry(message, LogSeverity.Warning, paramList);
        }

        private static void LogEntry(string message, LogSeverity severity, params object[] paramList)
        {
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

            if (Kernel.Loggers != null)
            {
                foreach (var logger in Kernel.Loggers)
                {
                    logger.LogEntry(row);
                }
            }
        }

        private static string FormatMessage(string message, params object[] paramList)
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
    }
}
