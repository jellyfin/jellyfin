using System;
using System.Text;

namespace MediaBrowser.Model.Logging
{
    /// <summary>
    /// Class LogHelper
    /// </summary>
    public static class LogHelper
    {
        /// <summary>
        /// Gets the log message.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>StringBuilder.</returns>
        public static StringBuilder GetLogMessage(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }

            var messageText = new StringBuilder();

            messageText.AppendLine(exception.Message);

            messageText.AppendLine(exception.GetType().FullName);

            LogExceptionData(messageText, exception);

            messageText.AppendLine(exception.StackTrace ?? "No Stack Trace Available");

            // Log the InnerExceptions, if any
            AppendInnerExceptions(messageText, exception);

            messageText.AppendLine(string.Empty);

            return messageText;
        }

        /// <summary>
        /// Appends the inner exceptions.
        /// </summary>
        /// <param name="messageText">The message text.</param>
        /// <param name="e">The e.</param>
        private static void AppendInnerExceptions(StringBuilder messageText, Exception e)
        {
            var aggregate = e as AggregateException;

            if (aggregate != null && aggregate.InnerExceptions != null)
            {
                foreach (var ex in aggregate.InnerExceptions)
                {
                    AppendInnerException(messageText, ex);
                    AppendInnerExceptions(messageText, ex);
                }
            }

            else if (e.InnerException != null)
            {
                AppendInnerException(messageText, e.InnerException);
                AppendInnerExceptions(messageText, e.InnerException);
            }
        }

        /// <summary>
        /// Appends the inner exception.
        /// </summary>
        /// <param name="messageText">The message text.</param>
        /// <param name="e">The e.</param>
        private static void AppendInnerException(StringBuilder messageText, Exception e)
        {
            messageText.AppendLine("InnerException: " + e.GetType().FullName);
            messageText.AppendLine(e.Message);

            LogExceptionData(messageText, e);

            if (e.StackTrace != null)
            {
                messageText.AppendLine(e.StackTrace);
            }
        }

        /// <summary>
        /// Logs the exception data.
        /// </summary>
        /// <param name="messageText">The message text.</param>
        /// <param name="e">The e.</param>
        private static void LogExceptionData(StringBuilder messageText, Exception e)
        {
            foreach (var key in e.Data.Keys)
            {
                messageText.AppendLine(key + ": " + e.Data[key]);
            }
        }
    }
}
