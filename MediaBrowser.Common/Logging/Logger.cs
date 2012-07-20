using System;

namespace MediaBrowser.Common.Logging
{
    public static class Logger
    {
        public static BaseLogger LoggerInstance { get; set; }

        public static void LogInfo(string message, params object[] paramList)
        {
            LoggerInstance.LogInfo(message, paramList);
        }

        public static void LogDebugInfo(string message, params object[] paramList)
        {
            LoggerInstance.LogDebugInfo(message, paramList);
        }

        public static void LogError(string message, params object[] paramList)
        {
            LoggerInstance.LogError(message, paramList);
        }

        public static void LogException(string message, Exception ex, params object[] paramList)
        {
            LoggerInstance.LogException(message, ex, paramList);
        }

        public static void LogWarning(string message, params object[] paramList)
        {
            LoggerInstance.LogWarning(message, paramList);
        }
    }
}
