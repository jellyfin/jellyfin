#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.IO
{
    public sealed class FileRefresher : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _configurationManager;

        private readonly List<string> _affectedPaths = new();
        private readonly Lock _timerLock = new();
        private Timer? _timer;
        private bool _disposed;

        public FileRefresher(string path, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogger logger)
        {
            logger.LogDebug("New file refresher created for {0}", path);
            Path = path;

            _configurationManager = configurationManager;
            _libraryManager = libraryManager;
            _logger = logger;
            AddPath(path);
        }

        public event EventHandler<EventArgs>? Completed;

        public string Path { get; private set; }

        private void AddAffectedPath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            if (!_affectedPaths.Contains(path, StringComparer.Ordinal))
            {
                _affectedPaths.Add(path);
            }
        }

        public void AddPath(string path)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);

            lock (_timerLock)
            {
                AddAffectedPath(path);
            }

            RestartTimer();
        }

        public void RestartTimer()
        {
            if (_disposed)
            {
                return;
            }

            lock (_timerLock)
            {
                if (_disposed)
                {
                    return;
                }

                if (_timer is null)
                {
                    _timer = new Timer(OnTimerCallback, null, TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
                else
                {
                    _timer.Change(TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        public void ResetPath(string path, string? affectedFile)
        {
            lock (_timerLock)
            {
                _logger.LogDebug("Resetting file refresher from {0} to {1}", Path, path);

                Path = path;
                AddAffectedPath(path);

                if (!string.IsNullOrEmpty(affectedFile))
                {
                    AddAffectedPath(affectedFile);
                }
            }

            RestartTimer();
        }

        private void OnTimerCallback(object? state)
        {
            List<string> paths;

            lock (_timerLock)
            {
                paths = _affectedPaths.ToList();
            }

            _logger.LogDebug("Timer stopped.");

            DisposeTimer();
            Completed?.Invoke(this, EventArgs.Empty);

            try
            {
                ProcessPathChanges(paths);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing directory changes");
            }
        }

        private void ProcessPathChanges(List<string> paths)
        {
            IEnumerable<BaseItem> itemsToRefresh = paths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(GetAffectedBaseItem)
                .Where(item => item is not null)
                .DistinctBy(x => x!.Id)!;  // Removed null values in the previous .Where()

            foreach (var item in itemsToRefresh)
            {
                if (item is AggregateFolder)
                {
                    continue;
                }

                _logger.LogInformation("{Name} ({Path}) will be refreshed.", item.Name, item.Path);

                try
                {
                    item.ChangedExternally();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing {Name}", item.Name);
                }
            }
        }

        /// <summary>
        /// Gets the affected base item.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem? GetAffectedBaseItem(string path)
        {
            BaseItem? item = null;

            while (item is null && !string.IsNullOrEmpty(path))
            {
                item = _libraryManager.FindByPath(path, null);

                path = System.IO.Path.GetDirectoryName(path) ?? string.Empty;
            }

            if (item is not null)
            {
                // If the item has been deleted find the first valid parent that still exists
                while (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    item = item.GetOwner() ?? item.GetParent();

                    if (item is null)
                    {
                        break;
                    }
                }
            }

            return item;
        }

        private void DisposeTimer()
        {
            lock (_timerLock)
            {
                if (_timer is not null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DisposeTimer();
            _disposed = true;
        }
    }
}
