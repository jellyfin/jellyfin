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
    public class FileRefresher : IDisposable
    {
        private readonly ILogger _logger;
        private readonly ILibraryManager _libraryManager;
        private readonly IServerConfigurationManager _configurationManager;

        private readonly List<string> _affectedPaths = new List<string>();
        private readonly object _timerLock = new object();
        private Timer _timer;

        public FileRefresher(string path, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ILogger logger)
        {
            logger.LogDebug("New file refresher created for {0}", path);
            Path = path;

            _configurationManager = configurationManager;
            _libraryManager = libraryManager;
            _logger = logger;
            AddPath(path);
        }

        public event EventHandler<EventArgs> Completed;

        public string Path { get; private set; }

        private void AddAffectedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!_affectedPaths.Contains(path, StringComparer.Ordinal))
            {
                _affectedPaths.Add(path);
            }
        }

        public void AddPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

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

                if (_timer == null)
                {
                    _timer = new Timer(OnTimerCallback, null, TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
                else
                {
                    _timer.Change(TimeSpan.FromSeconds(_configurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        public void ResetPath(string path, string affectedFile)
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

        private void OnTimerCallback(object state)
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
                ProcessPathChanges(paths.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing directory changes");
            }
        }

        private void ProcessPathChanges(List<string> paths)
        {
            var itemsToRefresh = paths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(GetAffectedBaseItem)
                .Where(item => item != null)
                .GroupBy(x => x.Id)
                .Select(x => x.First());

            foreach (var item in itemsToRefresh)
            {
                if (item is AggregateFolder)
                {
                    continue;
                }

                _logger.LogInformation("{name} ({path}) will be refreshed.", item.Name, item.Path);

                try
                {
                    item.ChangedExternally();
                }
                catch (IOException ex)
                {
                    // For now swallow and log.
                    // Research item: If an IOException occurs, the item may be in a disconnected state (media unavailable)
                    // Should we remove it from it's parent?
                    _logger.LogError(ex, "Error refreshing {name}", item.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing {name}", item.Name);
                }
            }
        }

        /// <summary>
        /// Gets the affected base item.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>BaseItem.</returns>
        private BaseItem GetAffectedBaseItem(string path)
        {
            BaseItem item = null;

            while (item == null && !string.IsNullOrEmpty(path))
            {
                item = _libraryManager.FindByPath(path, null);

                path = System.IO.Path.GetDirectoryName(path);
            }

            if (item != null)
            {
                // If the item has been deleted find the first valid parent that still exists
                while (!Directory.Exists(item.Path) && !File.Exists(item.Path))
                {
                    item = item.GetOwner() ?? item.GetParent();

                    if (item == null)
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
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }

        private bool _disposed;
        public void Dispose()
        {
            _disposed = true;
            DisposeTimer();
        }
    }
}
