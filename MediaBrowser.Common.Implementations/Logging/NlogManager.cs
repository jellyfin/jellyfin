using MediaBrowser.Model.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;

namespace MediaBrowser.Common.Implementations.Logging
{
    /// <summary>
    /// Class NlogManager
    /// </summary>
    public class NlogManager : ILogManager
    {
        /// <summary>
        /// Occurs when [logger loaded].
        /// </summary>
        public event EventHandler LoggerLoaded;
        /// <summary>
        /// Gets or sets the log directory.
        /// </summary>
        /// <value>The log directory.</value>
        private string LogDirectory { get; set; }
        /// <summary>
        /// Gets or sets the log file prefix.
        /// </summary>
        /// <value>The log file prefix.</value>
        private string LogFilePrefix { get; set; }
        /// <summary>
        /// Gets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NlogManager" /> class.
        /// </summary>
        /// <param name="logDirectory">The log directory.</param>
        /// <param name="logFileNamePrefix">The log file name prefix.</param>
        public NlogManager(string logDirectory, string logFileNamePrefix)
        {
            LogDirectory = logDirectory;
            LogFilePrefix = logFileNamePrefix;
        }

        private LogSeverity _severity = LogSeverity.Debug;
        public LogSeverity LogSeverity
        {
            get
            {
                return _severity;
            }
            set
            {
                var changed = _severity != value;

                _severity = value;

                if (changed)
                {
                    UpdateLogLevel(value);
                }
            }
        }

        private void UpdateLogLevel(LogSeverity newLevel)
        {
            var level = GetLogLevel(newLevel);

            var rules = LogManager.Configuration.LoggingRules;

            foreach (var rule in rules)
            {
                if (!rule.IsLoggingEnabledForLevel(level))
                {
                    rule.EnableLoggingForLevel(level);
                }
            }
        }

        /// <summary>
        /// Adds the file target.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="level">The level.</param>
        private void AddFileTarget(string path, LogSeverity level)
        {
            var logFile = new FileTarget
            {
                FileName = path,
                Layout = "${longdate}, ${level}, ${logger}, ${message}"
            };

            RemoveTarget("ApplicationLogFile");
            logFile.Name = "ApplicationLogFile";

            AddLogTarget(logFile, level);
        }

        /// <summary>
        /// Adds the log target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="level">The level.</param>
        public void AddLogTarget(Target target, LogSeverity level)
        {
            var config = LogManager.Configuration;
            config.AddTarget(target.Name, target);

            var rule = new LoggingRule("*", GetLogLevel(level), target);
            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;
        }

        /// <summary>
        /// Removes the target.
        /// </summary>
        /// <param name="name">The name.</param>
        public void RemoveTarget(string name)
        {
            var config = LogManager.Configuration;

            var target = config.FindTargetByName(name);

            if (target != null)
            {
                foreach (var rule in config.LoggingRules.ToList())
                {
                    var contains = rule.Targets.Contains(target);

                    rule.Targets.Remove(target);

                    if (contains)
                    {
                        config.LoggingRules.Remove(rule);
                    }
                }

                config.RemoveTarget(name);
                LogManager.Configuration = config;
            }
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ILogger.</returns>
        public ILogger GetLogger(string name)
        {
            return new NLogger(name);
        }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <returns>LogLevel.</returns>
        /// <exception cref="System.ArgumentException">Unrecognized LogSeverity</exception>
        private LogLevel GetLogLevel(LogSeverity severity)
        {
            switch (severity)
            {
                case LogSeverity.Debug:
                    return LogLevel.Debug;
                case LogSeverity.Error:
                    return LogLevel.Error;
                case LogSeverity.Fatal:
                    return LogLevel.Fatal;
                case LogSeverity.Info:
                    return LogLevel.Info;
                case LogSeverity.Warn:
                    return LogLevel.Warn;
                default:
                    throw new ArgumentException("Unrecognized LogSeverity");
            }
        }

        /// <summary>
        /// Reloads the logger.
        /// </summary>
        /// <param name="level">The level.</param>
        public void ReloadLogger(LogSeverity level)
        {
            LogFilePath = Path.Combine(LogDirectory, LogFilePrefix + "-" + decimal.Round(DateTime.Now.Ticks / 10000000) + ".log");

            AddFileTarget(LogFilePath, level);

            LogSeverity = level;

            if (LoggerLoaded != null)
            {
                try
                {
                    LoggerLoaded(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    GetLogger("Logger").ErrorException("Error in LoggerLoaded event", ex);
                }
            }
        }

        /// <summary>
        /// Flushes this instance.
        /// </summary>
        public void Flush()
        {
            LogManager.Flush();
        }
    }
}
