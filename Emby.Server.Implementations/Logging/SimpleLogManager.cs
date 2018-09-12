using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Logging;

namespace Emby.Server.Implementations.Logging
{
    public class SimpleLogManager : ILogManager, IDisposable
    {
        public LogSeverity LogSeverity { get; set; }
        public string ExceptionMessagePrefix { get; set; }
        private FileLogger _fileLogger;

        private readonly string LogDirectory;
        private readonly string LogFilePrefix;
        public string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

        public SimpleLogManager(string logDirectory, string logFileNamePrefix)
        {
            LogDirectory = logDirectory;
            LogFilePrefix = logFileNamePrefix;
        }

        public ILogger GetLogger(string name)
        {
            return new NamedLogger(name, this);
        }

        public async Task ReloadLogger(LogSeverity severity, CancellationToken cancellationToken)
        {
            LogSeverity = severity;

            var logger = _fileLogger;
            if (logger != null)
            {
                logger.Dispose();
                await TryMoveToArchive(logger.Path, cancellationToken).ConfigureAwait(false);
            }

            var newPath = Path.Combine(LogDirectory, LogFilePrefix + ".txt");

            if (File.Exists(newPath))
            {
                newPath = await TryMoveToArchive(newPath, cancellationToken).ConfigureAwait(false);
            }

            _fileLogger = new FileLogger(newPath);

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

        private async Task<string> TryMoveToArchive(string file, CancellationToken cancellationToken, int retryCount = 0)
        {
            var archivePath = GetArchiveFilePath();

            try
            {
                File.Move(file, archivePath);

                return file;
            }
            catch (FileNotFoundException)
            {
                return file;
            }
            catch (DirectoryNotFoundException)
            {
                return file;
            }
            catch
            {
                if (retryCount >= 50)
                {
                    return GetArchiveFilePath();
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);

                return await TryMoveToArchive(file, cancellationToken, retryCount + 1).ConfigureAwait(false);
            }
        }

        private string GetArchiveFilePath()
        {
            return Path.Combine(LogDirectory, LogFilePrefix + "-" + decimal.Floor(DateTime.Now.Ticks / 10000000) + ".txt");
        }

        public event EventHandler LoggerLoaded;

        public void Flush()
        {
            var logger = _fileLogger;
            if (logger != null)
            {
                logger.Flush();
            }
        }

        private bool _console = true;
        public void AddConsoleOutput()
        {
            _console = true;
        }

        public void RemoveConsoleOutput()
        {
            _console = false;
        }

        public void Log(string message)
        {
            if (_console)
            {
                Console.WriteLine(message);
            }

            var logger = _fileLogger;
            if (logger != null)
            {
                message = DateTime.Now.ToString(DateTimeFormat) + " " + message;

                logger.Log(message);
            }
        }

        public void Dispose()
        {
            var logger = _fileLogger;
            if (logger != null)
            {
                logger.Dispose();

                var task = TryMoveToArchive(logger.Path, CancellationToken.None);
                Task.WaitAll(task);
            }

            _fileLogger = null;
        }
    }

    public class FileLogger : IDisposable
    {
        private readonly FileStream _fileStream;

        private bool _disposed;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<string> _queue = new BlockingCollection<string>();

        public string Path { get; set; }

        public FileLogger(string path)
        {
            Path = path;

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));

            _fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 32768);
            _cancellationTokenSource = new CancellationTokenSource();

            Task.Factory.StartNew(LogInternal, _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void LogInternal()
        {
            while (!_cancellationTokenSource.IsCancellationRequested && !_disposed)
            {
                try
                {
                    foreach (var message in _queue.GetConsumingEnumerable())
                    {
                        var bytes = Encoding.UTF8.GetBytes(message + Environment.NewLine);
                        if (_disposed)
                        {
                            return;
                        }

                        _fileStream.Write(bytes, 0, bytes.Length);
                        if (_disposed)
                        {
                            return;
                        }

                        _fileStream.Flush(true);
                    }
                }
                catch
                {

                }
            }
        }

        public void Log(string message)
        {
            if (_disposed)
            {
                return;
            }

            _queue.Add(message);
        }

        public void Flush()
        {
            if (_disposed)
            {
                return;
            }

            _fileStream.Flush(true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _cancellationTokenSource.Cancel();

            var stream = _fileStream;
            if (stream != null)
            {
                using (stream)
                {
                    stream.Flush(true);
                }
            }
        }
    }

    public class NamedLogger : ILogger
    {
        public string Name { get; private set; }
        private readonly SimpleLogManager _logManager;

        public NamedLogger(string name, SimpleLogManager logManager)
        {
            Name = name;
            _logManager = logManager;
        }

        public void Info(string message, params object[] paramList)
        {
            Log(LogSeverity.Info, message, paramList);
        }

        public void Error(string message, params object[] paramList)
        {
            Log(LogSeverity.Error, message, paramList);
        }

        public void Warn(string message, params object[] paramList)
        {
            Log(LogSeverity.Warn, message, paramList);
        }

        public void Debug(string message, params object[] paramList)
        {
            if (_logManager.LogSeverity == LogSeverity.Info)
            {
                return;
            }
            Log(LogSeverity.Debug, message, paramList);
        }

        public void Fatal(string message, params object[] paramList)
        {
            Log(LogSeverity.Fatal, message, paramList);
        }

        public void FatalException(string message, Exception exception, params object[] paramList)
        {
            ErrorException(message, exception, paramList);
        }

        public void ErrorException(string message, Exception exception, params object[] paramList)
        {
            LogException(LogSeverity.Error, message, exception, paramList);
        }

        private void LogException(LogSeverity level, string message, Exception exception, params object[] paramList)
        {
            message = FormatMessage(message, paramList).Replace(Environment.NewLine, ". ");

            var messageText = LogHelper.GetLogMessage(exception);

            var prefix = _logManager.ExceptionMessagePrefix;

            if (!string.IsNullOrWhiteSpace(prefix))
            {
                messageText.Insert(0, prefix);
            }

            LogMultiline(message, level, messageText);
        }

        private static string FormatMessage(string message, params object[] paramList)
        {
            if (paramList != null)
            {
                for (var i = 0; i < paramList.Length; i++)
                {
                    var obj = paramList[i];

                    message = message.Replace("{" + i + "}", (obj == null ? "null" : obj.ToString()));
                }
            }

            return message;
        }

        public void LogMultiline(string message, LogSeverity severity, StringBuilder additionalContent)
        {
            if (severity == LogSeverity.Debug && _logManager.LogSeverity == LogSeverity.Info)
            {
                return;
            }

            additionalContent.Insert(0, message + Environment.NewLine);

            const char tabChar = '\t';

            var text = additionalContent.ToString()
                .Replace(Environment.NewLine, Environment.NewLine + tabChar)
                .TrimEnd(tabChar);

            if (text.EndsWith(Environment.NewLine))
            {
                text = text.Substring(0, text.LastIndexOf(Environment.NewLine, StringComparison.OrdinalIgnoreCase));
            }

            Log(severity, text);
        }

        public void Log(LogSeverity severity, string message, params object[] paramList)
        {
            message = severity + " " + Name + ": " + FormatMessage(message, paramList);

            _logManager.Log(message);
        }
    }
}
