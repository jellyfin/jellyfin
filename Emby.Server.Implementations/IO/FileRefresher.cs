using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Progress;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.System;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Model.Threading;

namespace Emby.Server.Implementations.IO
{
    public class FileRefresher : IDisposable
    {
        private ILogger Logger { get; set; }
        private ITaskManager TaskManager { get; set; }
        private ILibraryManager LibraryManager { get; set; }
        private IServerConfigurationManager ConfigurationManager { get; set; }
        private readonly IFileSystem _fileSystem;
        private readonly List<string> _affectedPaths = new List<string>();
        private ITimer _timer;
        private readonly ITimerFactory _timerFactory;
        private readonly object _timerLock = new object();
        public string Path { get; private set; }

        public event EventHandler<EventArgs> Completed;
        private readonly IEnvironmentInfo _environmentInfo;
        private readonly ILibraryManager _libraryManager;

        public FileRefresher(string path, IFileSystem fileSystem, IServerConfigurationManager configurationManager, ILibraryManager libraryManager, ITaskManager taskManager, ILogger logger, ITimerFactory timerFactory, IEnvironmentInfo environmentInfo, ILibraryManager libraryManager1)
        {
            logger.Debug("New file refresher created for {0}", path);
            Path = path;

            _fileSystem = fileSystem;
            ConfigurationManager = configurationManager;
            LibraryManager = libraryManager;
            TaskManager = taskManager;
            Logger = logger;
            _timerFactory = timerFactory;
            _environmentInfo = environmentInfo;
            _libraryManager = libraryManager1;
            AddPath(path);
        }

        private void AddAffectedPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            if (!_affectedPaths.Contains(path, StringComparer.Ordinal))
            {
                _affectedPaths.Add(path);
            }
        }

        public void AddPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
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
                    _timer = _timerFactory.Create(OnTimerCallback, null, TimeSpan.FromSeconds(ConfigurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
                else
                {
                    _timer.Change(TimeSpan.FromSeconds(ConfigurationManager.Configuration.LibraryMonitorDelay), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        public void ResetPath(string path, string affectedFile)
        {
            lock (_timerLock)
            {
                Logger.Debug("Resetting file refresher from {0} to {1}", Path, path);

                Path = path;
                AddAffectedPath(path);

                if (!string.IsNullOrWhiteSpace(affectedFile))
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

            Logger.Debug("Timer stopped.");

            DisposeTimer();
            EventHelper.FireEventIfNotNull(Completed, this, EventArgs.Empty, Logger);

            try
            {
                ProcessPathChanges(paths.ToList());
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error processing directory changes", ex);
            }
        }

        private void ProcessPathChanges(List<string> paths)
        {
            var itemsToRefresh = paths
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(GetAffectedBaseItem)
                .Where(item => item != null)
                .DistinctBy(i => i.Id)
                .ToList();

            //foreach (var p in paths)
            //{
            //    Logger.Info(p + " reports change.");
            //}

            // If the root folder changed, run the library task so the user can see it
            if (itemsToRefresh.Any(i => i is AggregateFolder))
            {
                LibraryManager.ValidateMediaLibrary(new SimpleProgress<double>(), CancellationToken.None);
                return;
            }

            foreach (var item in itemsToRefresh)
            {
                Logger.Info(item.Name + " (" + item.Path + ") will be refreshed.");

                try
                {
                    item.ChangedExternally();
                }
                catch (IOException ex)
                {
                    // For now swallow and log. 
                    // Research item: If an IOException occurs, the item may be in a disconnected state (media unavailable)
                    // Should we remove it from it's parent?
                    Logger.ErrorException("Error refreshing {0}", ex, item.Name);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error refreshing {0}", ex, item.Name);
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
                item = LibraryManager.FindByPath(path, null);

                path = _fileSystem.GetDirectoryName(path);
            }

            if (item != null)
            {
                // If the item has been deleted find the first valid parent that still exists
                while (!_fileSystem.DirectoryExists(item.Path) && !_fileSystem.FileExists(item.Path))
                {
                    item = item.IsOwnedItem ? item.GetOwner() : item.GetParent();

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
            GC.SuppressFinalize(this);
        }
    }
}
