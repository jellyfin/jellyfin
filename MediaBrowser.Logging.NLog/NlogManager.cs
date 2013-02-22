using NLog;
using NLog.Config;
using NLog.Targets;

namespace MediaBrowser.Logging.Nlog
{
    /// <summary>
    /// Class NlogManager
    /// </summary>
    public static class NlogManager
    {
        /// <summary>
        /// Adds the file target.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="enableDebugLogging">if set to <c>true</c> [enable debug logging].</param>
        public static void AddFileTarget(string path, bool enableDebugLogging)
        {
            var logFile = new FileTarget();

            logFile.FileName = path;
            logFile.Layout = "${longdate}, ${level}, ${logger}, ${message}";

            AddLogTarget(logFile, "ApplicationLogFile", enableDebugLogging);
        }

        /// <summary>
        /// Adds the log target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="name">The name.</param>
        /// <param name="enableDebugLogging">if set to <c>true</c> [enable debug logging].</param>
        private static void AddLogTarget(Target target, string name, bool enableDebugLogging)
        {
            var config = LogManager.Configuration;

            config.RemoveTarget(name);

            target.Name = name;
            config.AddTarget(name, target);

            var level = enableDebugLogging ? LogLevel.Debug : LogLevel.Info;

            var rule = new LoggingRule("*", level, target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
        }
    }
}
