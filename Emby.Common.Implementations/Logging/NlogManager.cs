using System;
using System.IO;
using System.Linq;
using System.Xml;
using NLog;
using NLog.Config;
using NLog.Filters;
using NLog.Targets;
using NLog.Targets.Wrappers;
using MediaBrowser.Model.Logging;

namespace Emby.Common.Implementations.Logging
{
    /// <summary>
    /// Class NlogManager
    /// </summary>
    public class NlogManager : ILogManager
    {
        #region Private Fields

        private LogSeverity _severity = LogSeverity.Debug;

        /// <summary>
        /// Gets or sets the log directory.
        /// </summary>
        /// <value>The log directory.</value>
        private readonly string LogDirectory;

        /// <summary>
        /// Gets or sets the log file prefix.
        /// </summary>
        /// <value>The log file prefix.</value>
        private readonly string LogFilePrefix;

        #endregion

        #region Event Declarations

        /// <summary>
        /// Occurs when [logger loaded].
        /// </summary>
        public event EventHandler LoggerLoaded;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the log file path.
        /// </summary>
        /// <value>The log file path.</value>
        public string LogFilePath { get; private set; }

        /// <summary>
        /// Gets or sets the exception message prefix.
        /// </summary>
        /// <value>The exception message prefix.</value>
        public string ExceptionMessagePrefix { get; set; }

        public string NLogConfigurationFilePath { get; set; }

        public LogSeverity LogSeverity
        {

            get
            {
                return _severity;
            }

            set
            {
                DebugFileWriter(
                    LogDirectory, String.Format(
                    "SET LogSeverity, _severity = [{0}], value = [{1}]",
                    _severity.ToString(),
                    value.ToString()
                ));

                var changed = _severity != value;

                _severity = value;

                if (changed)
                {
                    UpdateLogLevel(value);
                }

            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="NlogManager" /> class.
        /// </summary>
        /// <param name="logDirectory">The log directory.</param>
        /// <param name="logFileNamePrefix">The log file name prefix.</param>
        public NlogManager(string logDirectory, string logFileNamePrefix)
        {
            DebugFileWriter(
                logDirectory, String.Format(
                "NlogManager constructor called, logDirectory is [{0}], logFileNamePrefix is [{1}], _severity is [{2}].",
                logDirectory,
                logFileNamePrefix,
                _severity.ToString()
            ));

            LogDirectory = logDirectory;
            LogFilePrefix = logFileNamePrefix;

            LogManager.Configuration = new LoggingConfiguration();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NlogManager" /> class.
        /// </summary>
        /// <param name="logDirectory">The log directory.</param>
        /// <param name="logFileNamePrefix">The log file name prefix.</param>
        public NlogManager(string logDirectory, string logFileNamePrefix, LogSeverity initialSeverity) : this(logDirectory, logFileNamePrefix)
        {
            _severity = initialSeverity;

            DebugFileWriter(
                logDirectory, String.Format(
                "NlogManager constructor called, logDirectory is [{0}], logFileNamePrefix is [{1}], _severity is [{2}].",
                logDirectory,
                logFileNamePrefix,
                _severity.ToString()
            ));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds the file target.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="level">The level.</param>
        private void AddFileTarget(string path, LogSeverity level)
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "AddFileTarget called, path = [{0}], level = [{1}].",
                path,
                level.ToString()
            ));

            RemoveTarget("ApplicationLogFileWrapper");

            var wrapper = new AsyncTargetWrapper();
            wrapper.Name = "ApplicationLogFileWrapper";

            var logFile = new FileTarget
            {
                FileName = path,
                Layout = "${longdate} ${level} ${logger}: ${message}"
            };

            logFile.Name = "ApplicationLogFile";

            wrapper.WrappedTarget = logFile;

            AddLogTarget(wrapper, level);

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

        private void UpdateLogLevel(LogSeverity newLevel)
        {
            DebugFileWriter(
                LogDirectory, String.Format(
                "UpdateLogLevel called, newLevel = [{0}].",
                newLevel.ToString()
            ));

            var level = GetLogLevel(newLevel);

            var rules = LogManager.Configuration.LoggingRules;

            foreach (var rule in rules)
            {
                if (!rule.IsLoggingEnabledForLevel(level))
                {
                    rule.EnableLoggingForLevel(level);
                }
                foreach (var lev in rule.Levels.ToArray())
                {
                    if (lev < level)
                    {
                        rule.DisableLoggingForLevel(lev);
                    }
                }
            }
        }

        private void AddCustomFilters(string defaultLoggerNamePattern, LoggingRule defaultRule)
        {
            DebugFileWriter(
                LogDirectory, String.Format(
                "AddCustomFilters called, defaultLoggerNamePattern = [{0}], defaultRule.LoggerNamePattern = [{1}].",
                defaultLoggerNamePattern,
                defaultRule.LoggerNamePattern
            ));

            try
            {
                var customConfig = new NLog.Config.XmlLoggingConfiguration(NLogConfigurationFilePath);

                DebugFileWriter(
                    LogDirectory, String.Format(
                    "Custom Configuration Loaded, Rule Count = [{0}].",
                    customConfig.LoggingRules.Count.ToString()
                ));

                foreach (var customRule in customConfig.LoggingRules)
                {

                    DebugFileWriter(
                        LogDirectory, String.Format(
                        "Read Custom Rule, LoggerNamePattern = [{0}], Targets = [{1}].",
                        customRule.LoggerNamePattern,
                        string.Join(",", customRule.Targets.Select(x => x.Name).ToList())
                    ));

                    if (customRule.LoggerNamePattern.Equals(defaultLoggerNamePattern))
                    {

                        if (customRule.Targets.Any((arg) => arg.Name.Equals(defaultRule.Targets.First().Name)))
                        {

                            DebugFileWriter(
                                LogDirectory, String.Format(
                                "Custom rule filters can be applied to this target, Filter Count = [{0}].",
                                customRule.Filters.Count.ToString()
                            ));

                            foreach (ConditionBasedFilter customFilter in customRule.Filters)
                            {

                                DebugFileWriter(
                                    LogDirectory, String.Format(
                                    "Read Custom Filter, Filter = [{0}], Action = [{1}], Type = [{2}].",
                                    customFilter.Condition.ToString(),
                                    customFilter.Action.ToString(),
                                    customFilter.GetType().ToString()
                                ));

                                defaultRule.Filters.Add(customFilter);

                            }
                        }
                        else
                        {

                            DebugFileWriter(
                                LogDirectory, String.Format(
                                "Ignoring custom rule as [Target] does not match."
                            ));

                        }

                    }
                    else
                    {

                        DebugFileWriter(
                            LogDirectory, String.Format(
                            "Ignoring custom rule as [LoggerNamePattern] does not match."
                        ));

                    }
                }
            }
            catch (Exception ex)
            {
                // Intentionally do nothing, prevent issues affecting normal execution.
                DebugFileWriter(
                    LogDirectory, String.Format(
                        "Exception in AddCustomFilters, ex.Message = [{0}].",
                        ex.Message
                    )
                );

            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ILogger.</returns>
        public MediaBrowser.Model.Logging.ILogger GetLogger(string name)
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "GetLogger called, name = [{0}].",
                name
            ));

            return new NLogger(name, this);

        }

        /// <summary>
        /// Adds the log target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="level">The level.</param>
        public void AddLogTarget(Target target, LogSeverity level)
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "AddLogTarget called, target.Name = [{0}], level = [{1}].",
                target.Name,
                level.ToString()
            ));

            string loggerNamePattern = "*";
            var config = LogManager.Configuration;
            var rule = new LoggingRule(loggerNamePattern, GetLogLevel(level), target);

            config.AddTarget(target.Name, target);

            AddCustomFilters(loggerNamePattern, rule);

            config.LoggingRules.Add(rule);

            LogManager.Configuration = config;

        }

        /// <summary>
        /// Removes the target.
        /// </summary>
        /// <param name="name">The name.</param>
        public void RemoveTarget(string name)
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "RemoveTarget called, name = [{0}].",
                name
            ));

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

        public void AddConsoleOutput()
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "AddConsoleOutput called."
            ));

            RemoveTarget("ConsoleTargetWrapper");

            var wrapper = new AsyncTargetWrapper();
            wrapper.Name = "ConsoleTargetWrapper";

            var target = new ConsoleTarget()
            {
                Layout = "${level}, ${logger}, ${message}",
                Error = false
            };

            target.Name = "ConsoleTarget";

            wrapper.WrappedTarget = target;

            AddLogTarget(wrapper, LogSeverity);

        }

        public void RemoveConsoleOutput()
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "RemoveConsoleOutput called."
            ));

            RemoveTarget("ConsoleTargetWrapper");

        }

        /// <summary>
        /// Reloads the logger, maintaining the current log level.
        /// </summary>
        public void ReloadLogger()
        {
            ReloadLogger(LogSeverity);
        }

        /// <summary>
        /// Reloads the logger, using the specified logging level.
        /// </summary>
        /// <param name="level">The level.</param>
        public void ReloadLogger(LogSeverity level)
        {

            DebugFileWriter(
                LogDirectory, String.Format(
                "ReloadLogger called, level = [{0}], LogFilePath (existing) = [{1}].",
                level.ToString(),
                LogFilePath
            ));

            LogFilePath = Path.Combine(LogDirectory, LogFilePrefix + "-" + decimal.Floor(DateTime.Now.Ticks / 10000000) + ".txt");

            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath));

            AddFileTarget(LogFilePath, level);

            LogSeverity = level;

            if (LoggerLoaded != null)
            {
                try
                {

                    DebugFileWriter(
                        LogDirectory, String.Format(
                        "ReloadLogger called, raised event LoggerLoaded."
                    ));

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

            DebugFileWriter(
                LogDirectory, String.Format(
                "Flush called."
            ));

            LogManager.Flush();

        }

        #endregion

        #region Conditional Debug Methods

        /// <summary>
        /// DEBUG: Standalone method to write out debug to assist with logger development/troubleshooting.
        /// <list type="bullet">
        /// <item><description>The output file will be written to the server's log directory.</description></item>
        /// <item><description>Calls to the method are safe and will never throw any exceptions.</description></item>
        /// <item><description>Method calls will be omitted unless the library is compiled with DEBUG defined.</description></item>
        /// </list>
        /// </summary>
        private static void DebugFileWriter(string logDirectory, string message)
        {
#if DEBUG
            try
            {

                System.IO.File.AppendAllText(
                    Path.Combine(logDirectory, "NlogManager.txt"),
                    String.Format(
                        "{0} : {1}{2}",
                        System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                        message,
                        System.Environment.NewLine
                    )
                );

            }
            catch (Exception ex)
            {
                // Intentionally do nothing, prevent issues affecting normal execution.
            }
#endif
        }
        #endregion
    }
}